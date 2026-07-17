declare type InteropCallback<T = void> = {
  invokeMethodAsync(methodName: 'Invoke', arg?: T): Promise<void>;
};

declare type Station = {
  bitrate?: number;
  countryCode?: string;
  iconUrl?: string;
  id: string;
  isHls: boolean;
  name: string;
  url: string;
};