import Hls, { ErrorData } from 'hls.js/dist/hls.mjs';
import { HubConnectionBuilder, ISubscription } from '@microsoft/signalr';

export function createPlayer(options: PlayerAudioOptions, events: StationPlayerEvents) {
  return new StationPlayer(options, events);
};

function debounce<T = void>(callback: (...args: any[]) => T, wait: number, controller?: AbortController): (...args: any[]) => void {
  let timeoutId: number;
  controller?.signal.addEventListener('abort', () => window.clearTimeout(timeoutId), {
    once: true,
    passive: true
  });

  return (...args) => {
    const later = () => {
      window.clearTimeout(timeoutId);
      return callback(...args);
    };

    window.clearTimeout(timeoutId);
    timeoutId = window.setTimeout(later, wait);
  };
};

function normalizeTitle(value: string | null): string | undefined {
  if (!value?.length) {
    return;
  }

  value = value.trim();
  if (!value.length || value === '-') {
    return;
  }

  return value;
}

const MAX_CUE_ENDTIME = (() => {
  const Cue = VTTCue;
  try {
    Cue && new Cue(0, Number.POSITIVE_INFINITY, '');
  } catch (e) {
    return Number.MAX_VALUE;
  }
  return Number.POSITIVE_INFINITY;
})();

class IcecastMetadataWriter {
  private audio: HTMLAudioElement;
  private hub = new HubConnectionBuilder()
    .withAutomaticReconnect()
    .withUrl(`/api/signals/metadata`)
    .build();

  private subscription?: ISubscription<{ [key: string]: string; }>;

  constructor(audio: HTMLAudioElement) {
    this.audio = audio;
  }

  async connect(station: Station) {
    this.close();
    await this.hub.start();

    const controller = new AbortController();
    const subscription = this.hub.stream('Metadata', station.id).subscribe({
      complete: () => this.disconnect(),
      error: () => this.disconnect(),
      next: debounce(metadata => {
        if (controller.signal.aborted) {
          return;
        }

        return this.onNextMetadata(station, metadata);
      }, 175, controller)
    });

    this.subscription = {
      dispose() {
        controller.abort();
        subscription.dispose();
      }
    };
  }

  close() {
    if (this.subscription) {
      this.subscription.dispose();
      this.subscription = undefined;
    }

    const track = this.getMetadataTrack();
    if (track.cues?.length) {
      for (let i = 0; i < track.cues.length; i++) {
        const cue = track.cues[i] as MetadataCue;

        cue.startTime = -1;
        cue.endTime = 0;
      }
    }
  }

  async disconnect() {
    this.close();
    return this.hub.stop();
  }

  private getMetadataTrack() {
    const track = [...this.audio.textTracks].find(track => track.kind === 'metadata' && track.label === MetadataType.Icy)
      || this.audio.addTextTrack('metadata', MetadataType.Icy);

    if (track.mode !== 'hidden') {
      track.mode = 'hidden';
    }

    return track;
  }

  private onNextMetadata(station: Station, metadata: { [key: string]: string; }) {
    const keys = Object.keys(metadata);
    if (!keys.length) {
      return;
    }

    const start = Math.max(0, this.audio.currentTime);
    const end = station.bitrate ? start + (Number.parseInt(metadata.interval) * 8) / (station.bitrate * 1000) : MAX_CUE_ENDTIME;

    const track = this.getMetadataTrack();
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
  }
}

class StationPlayer extends EventTarget {
  private audio: HTMLAudioElement;
  private hls?: Hls;
  private icecast: IcecastMetadataWriter;
  private metadata: MediaMetadata | null = null;
  private station?: Station;

  constructor(options: PlayerAudioOptions, events: StationPlayerEvents) {
    super();

    this.audio = new Audio();
    this.audio.controls = false;
    this.audio.preload = 'auto';
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
            const meta: MediaMetadataInit = {};

            for (let i = track.activeCues.length; i--;) {
              const cue = track.activeCues[i] as MetadataCue;
              if (cue.startTime < 0 || cue.endTime <= 0) {
                continue;
              }

              if (cue.value.key === 'streamTitle') {
                meta.title = normalizeTitle(cue.value.data);
              }
            }

            return this.dispatchEvent(new CustomEvent('metachange', {
              detail: meta
            }));
          }

          case MetadataType.Id3: {
            const meta: MediaMetadataInit = {};

            for (let i = track.activeCues.length; i--;) {
              const cue = track.activeCues[i] as MetadataCue;
              if (cue.startTime < 0 || cue.endTime <= 0) {
                continue;
              }

              if (cue.value.key === 'TAL' || cue.value.key === 'TALB') {
                meta.album = normalizeTitle(cue.value.data);
              }

              if (cue.value.key === 'TP1' || cue.value.key === 'TPE1') {
                meta.artist = normalizeTitle(cue.value.data);
              }

              if (cue.value.key === 'TT2' || cue.value.key === 'TIT2') {
                meta.title = normalizeTitle(cue.value.data);
              }
            }

            return this.dispatchEvent(new CustomEvent('metachange', {
              detail: meta
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

    this.icecast = new IcecastMetadataWriter(this.audio);
    this.addEventListener('metachange', e => {
      const init = (e as CustomEvent).detail as MediaMetadataInit;
      if (init && (this.station?.iconUrl?.length && !init.artwork?.length)) {
        init.artwork = [{ src: this.station?.iconUrl }];
      }

      const meta = new MediaMetadata(init);
      if (this.metadata !== meta) {
        this.metadata = meta;
        if ('mediaSession' in navigator) {
          navigator.mediaSession.metadata = meta;
        }

        return events.invokeMethodAsync('OnMetaChanged', {
          meta: {
            ...init,
            updatedAt: new Date(),
          },
          stationId: this.station?.id
        });
      }
    }, { passive: true });

    if ('mediaSession' in navigator) {
      navigator.mediaSession.setActionHandler('nexttrack', null);
      navigator.mediaSession.setActionHandler('pause', () => events.invokeMethodAsync('OnStop'));
      navigator.mediaSession.setActionHandler('play', null);
      navigator.mediaSession.setActionHandler('previoustrack', null);
      navigator.mediaSession.setActionHandler('seekbackward', null);
      navigator.mediaSession.setActionHandler('seekforward', null);
      navigator.mediaSession.setActionHandler('seekto', null);
      navigator.mediaSession.setActionHandler('skipad', null);
      navigator.mediaSession.setActionHandler('stop', () => events.invokeMethodAsync('OnStop'));
    }
  }

  async dispose() {
    await this.stop();
    await this.icecast.disconnect();

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

    this.station = station;
    return new Promise<void>((resolve, reject) => {
      this.audio.addEventListener('error', e => reject(e.error ?? e), {
        once: true,
        passive: true
      });

      this.audio.addEventListener('loadedmetadata', async () => {
        await this.audio.play();
        this.dispatchEvent(new CustomEvent('metachange', {
          detail: this.metadata = new MediaMetadata({
            artwork: station.iconUrl ? [{ src: station.iconUrl }] : undefined,
            title: station.name,
          })
        }));

        if ('mediaSession' in navigator) {
          navigator.mediaSession.metadata = this.metadata;
          navigator.mediaSession.playbackState = 'playing';
        }

        if (!station.isHls) {
          await this.icecast.connect(station);
        }

        return resolve();
      }, {
        once: true,
        passive: true
      });

      if (station.isHls) {
        if (!this.hls) {
          throw new HlsNotSupportedError();
        }

        const onError = (_: unknown, data: ErrorData) => {
          if (data.fatal) {
            return reject(data.error ?? data);
          }

          this.hls?.once(Hls.Events.ERROR, onError);
        };

        this.hls.once(Hls.Events.ERROR, onError);
        this.hls.once(Hls.Events.MEDIA_ATTACHED, (_, { }) => {
          this.hls!.loadSource(station.url);
        });

        return this.hls.attachMedia(this.audio);
      }

      this.audio.src = station.url;
      this.audio.load();
    });
  }

  async stop() {
    this.audio.pause();
    this.station = undefined;

    if ('mediaSession' in navigator) {
      navigator.mediaSession.playbackState = 'none';
    }

    if (this.hls) {
      this.hls.off(Hls.Events.ERROR);
      this.hls.stopLoad();
      this.hls.detachMedia();
    }

    await this.icecast.disconnect();
    if (this.metadata) {
      this.dispatchEvent(new CustomEvent('metachange', {
        detail: null
      }));
    }

    this.audio.src = undefined!;
    this.audio.removeAttribute('src');
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

type OnMetaChangedEvent = {
  meta: MediaMetadataInit & { updatedAt: Date } | null;
  stationId: string | undefined;
};

type PlayerAudioOptions = {
  muted: boolean;
  volume: number;
}

type StationPlayerEvents = {
  invokeMethodAsync<K extends keyof StationPlayerEventMap>(methodName: K, arg?: StationPlayerEventMap[K]): Promise<void>;
}

type StationPlayerEventMap = {
  'OnMetaChanged': OnMetaChangedEvent;
  'OnStop': void;
}

type Station = {
  bitrate?: number;
  countryCode?: string;
  iconUrl?: string;
  id: string;
  isHls: boolean;
  name: string;
  url: string;
};