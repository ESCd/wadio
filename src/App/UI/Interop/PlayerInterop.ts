import Hls from 'hls.js/dist/hls.mjs';

export function createAudio(options: PlayerAudioOptions) {
  return new PlayerAudio(options);
};

class PlayerAudio {
  private audio: HTMLAudioElement;
  private hls?: Hls;

  constructor(options: PlayerAudioOptions) {
    this.audio = new Audio()
    this.audio.controls = false;
    this.audio.preload = 'auto';

    if (options.muted) {
      this.audio.muted = true;
    }

    if (options.volume) {
      this.audio.volume = options.volume;
    }

    if (Hls.isSupported()) {
      this.hls = new Hls({
        enableID3MetadataCues: true,
        enableEmsgMetadataCues: true,
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
    });
  }

  async stop() {
    this.audio.pause();
    if (this.hls) {
      this.hls.stopLoad();
      this.hls.detachMedia();
    }

    this.audio.src = '';
  }

  volume(value: number) {
    return this.audio.volume = value;
  }
}

class HlsNotSupportedError extends Error {
  constructor() {
    super('HLS decoding is not supported by the current device.')
  }
}

type PlayerAudioOptions = {
  muted: boolean;
  volume: number;
};

type Station = {
  isHls: boolean;
  url: string;
};