export async function getCurrentPosition(resolve: InteropCallback<GeolocationCoordinates>, reject: InteropCallback<GeolocationError>, options?: PositionOptions) {
  if ('geolocation' in navigator) {
    return navigator.geolocation.getCurrentPosition(
      position => resolve.invokeMethodAsync('Invoke', position.coords),
      error => reject.invokeMethodAsync('Invoke', {
        code: error.code,
        message: error.message
      }), options);
  }

  return reject.invokeMethodAsync('Invoke', {
    code: GeolocationPositionError.POSITION_UNAVAILABLE,
    message: 'Geolocation is not supported by the device.'
  });
};

type GeolocationError = {
  code: number;
  message: string;
};