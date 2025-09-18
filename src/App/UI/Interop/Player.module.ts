import Hls from 'hls.js/dist/hls.mjs';
import { HubConnection, HubConnectionBuilder, HubConnectionState } from '@microsoft/signalr';

export function createAudio(options: PlayerAudioOptions, callback: PlayerTitleCallback) {
  return new PlayerAudio(options, title => callback.invokeMethodAsync('Invoke', title));
};

function debounce<T = void>(callback: (...args: any[]) => T, wait: number): (...args: any[]) => void {
  let timeoutId: number;
  return (...args) => {
    const later = () => {
      window.clearTimeout(timeoutId);
      return callback(...args);
    };

    window.clearTimeout(timeoutId);
    timeoutId = window.setTimeout(later, wait);
  };
};

function normalizeTitle(value: string | null): string | null {
  if (!value?.length) {
    return null;
  }

  value = value.trim();
  if (!value.length || value === '-') {
    return null;
  }

  return value;
}

class MetadataWriter {
  private hub?: Promise<HubConnection>;

  constructor(audio: HTMLAudioElement, station: Station) {
    const MAX_CUE_ENDTIME = (() => {
      const Cue = VTTCue;
      try {
        Cue && new Cue(0, Number.POSITIVE_INFINITY, '');
      } catch (e) {
        return Number.MAX_VALUE;
      }
      return Number.POSITIVE_INFINITY;
    })();

    const getMetadataTrack = () => {
      const track = [...audio.textTracks].find(track => track.kind === 'metadata' && track.label === MetadataType.Icy)
        || audio.addTextTrack('metadata', MetadataType.Icy);

      if (track.mode !== 'hidden') {
        track.mode = 'hidden';
      }

      return track;
    };

    const onMetadataReceived = (metadata: { [key: string]: string; }) => {
      if (!this?.hub) {
        return;
      }

      const keys = Object.keys(metadata);
      if (!keys.length) {
        return;
      }

      const start = Math.max(0, audio.currentTime);
      const end = station.bitrate ? start + (Number.parseInt(metadata.interval) * 8) / (station.bitrate * 1000) : MAX_CUE_ENDTIME;

      const track = getMetadataTrack();
      if (track.cues?.length) {
        for (let i = track.cues.length; i--;) {
          const cue = track.cues[i] as MetadataCue;
          if (cue.startTime < start && cue.endTime === MAX_CUE_ENDTIME) {
            cue.endTime = start;
          }
        }
      }

      for (const key of keys) {
        if (key === 'interval') continue;

        const cue = new VTTCue(start, end, '') as MetadataCue;
        cue.type = MetadataType.Icy;
        cue.value = {
          key,
          data: metadata[key]
        };

        track.addCue(cue);
      }
    };

    this.hub = new Promise<HubConnection>((resolve, reject) => {
      const hub = new HubConnectionBuilder()
        .withAutomaticReconnect()
        .withUrl(`/api/signals/metadata?stationId=${encodeURIComponent(station.id)}`)
        .build();

      hub.on('Metadata', debounce(onMetadataReceived, 125));
      return hub.start().then(() => resolve(hub)).catch(reject);
    });
  }

  async dispose() {
    await this.hub?.then(hub => {
      if (hub.state === HubConnectionState.Connected) {
        return hub.stop();
      }
    });

    delete this.hub;
  }
}

class PlayerAudio extends EventTarget {
  private audio: HTMLAudioElement;
  private hls?: Hls;
  private metadata?: MetadataWriter;
  private title?: string | null = null;

  constructor(options: PlayerAudioOptions, onTitleChange?: (title: string | null) => void) {
    super();

    this.audio = new Audio();
    this.audio.controls = false;
    this.audio.preload = 'auto';

    this.addEventListener('titlechange', e => {
      const title = (e as CustomEvent).detail as string;
      if (this.title !== title) {
        this.title = title;
        if (onTitleChange) {
          return onTitleChange(title);
        }
      }
    }, { passive: true });

    this.audio.textTracks.addEventListener('addtrack', e => {
      const { track } = e;
      if (track?.kind !== 'metadata') {
        return;
      }

      track.addEventListener('cuechange', () => {
        if (this.audio.paused || this.audio.currentTime < 0 || !track.activeCues?.length) {
          return;
        }

        switch (track.label) {
          case MetadataType.Icy: {
            const meta: { title: string | null; } = {
              title: null,
            };

            for (let i = track.activeCues.length; i--;) {
              const cue = track.activeCues[i] as MetadataCue;
              if (cue.startTime < 0 || cue.endTime <= 0) {
                continue;
              }

              if (cue.value.key === 'streamTitle') {
                meta.title = cue.value.data;
              }
            }

            return this.dispatchEvent(new CustomEvent('titlechange', {
              detail: normalizeTitle(meta.title)
            }));
          }

          case MetadataType.Id3: {
            const meta: { artist?: string, title?: string; } = {
              artist: undefined,
              title: undefined,
            };

            for (let i = track.activeCues.length; i--;) {
              const cue = track.activeCues[i] as MetadataCue;
              if (cue.startTime < 0 || cue.endTime <= 0) {
                continue;
              }

              if (cue.value.key === 'TPE1') {
                meta.artist = cue.value.data;
              }

              if (cue.value.key === 'TIT2') {
                meta.title = cue.value.data;
              }
            }

            return this.dispatchEvent(new CustomEvent('titlechange', {
              detail: normalizeTitle(`${meta.artist ?? ''}${meta.artist && meta.title ? ' - ' : ''}${meta.title ?? ''}`)
            }));
          }

          default:
            console.warn('Unexpected metadata track label:', track.label);
            break;
        }
      }, { passive: true });
    }, { passive: true });

    if (options.muted) {
      this.audio.muted = true;
    }

    if (options.volume) {
      this.audio.volume = options.volume;
    }

    if (Hls.isSupported()) {
      this.hls = new Hls({
        enableID3MetadataCues: true,
        progressive: true,
      });
    }
  }

  async dispose() {
    await this.stop();

    if (this.hls) {
      this.hls.destroy();
      delete this.hls;
    }
  }

  muted(value: boolean) {
    return this.audio.muted = value;
  }

  async play(station: Station, options: PlayerAudioOptions) {
    await this.stop();

    this.muted(options.muted);
    this.volume(options.volume);

    return new Promise((resolve, reject) => {
      this.audio.addEventListener('error', e => reject(e.error), {
        once: true,
        passive: true
      });

      this.audio.addEventListener('loadedmetadata', () => this.audio.play().then(resolve).catch(reject), {
        once: true,
        passive: true
      });

      if (station.isHls) {
        if (!this.hls) {
          throw new HlsNotSupportedError();
        }

        this.hls.once(Hls.Events.MEDIA_ATTACHED, (_, { }) => {
          this.hls!.loadSource(station.url);
        });

        return this.hls.attachMedia(this.audio);
      }

      this.audio.src = station.url;
      this.audio.load();
      this.metadata = new MetadataWriter(this.audio, station);
    });
  }

  async stop() {
    this.audio.pause();
    if (this.hls) {
      this.hls.stopLoad();
      this.hls.detachMedia();
    }

    if (this.metadata) {
      await this.metadata.dispose();
      delete this.metadata;
    }

    for (const track of this.audio.textTracks) {
      if (track.kind === 'metadata' && track.cues?.length) {
        for (let i = 0; i < track.cues.length; i++) {
          track.removeCue(track.cues[i]);
        }
      }
    }

    if (this.title?.length) {
      this.dispatchEvent(new CustomEvent('titlechange', {
        detail: null
      }));
    }

    this.audio.removeAttribute('src');
    this.audio.src = undefined!;
    this.audio.load();
  }

  volume(value: number) {
    return this.audio.volume = value;
  }
}

class HlsNotSupportedError extends Error {
  constructor() {
    super('HLS decoding is not supported on the current device.');
  }
}

type MetadataCue = VTTCue & {
  type: MetadataType;
  value: {
    key: string;
    data: string;
  };
}

enum MetadataType {
  Icy = 'icy',
  Id3 = 'id3'
}

type PlayerAudioOptions = {
  muted: boolean;
  volume: number;
}

type PlayerTitleCallback = {
  invokeMethodAsync(methodName: 'Invoke', title: string | null): Promise<void>;
}

type Station = {
  bitrate?: number;
  countryCode?: string;
  id: string;
  isHls: boolean;
  url: string;
};