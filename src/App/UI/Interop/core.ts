export function animate<T>(callback: (time: DOMHighResTimeStamp) => T) {
  return new Promise<T>(resolve => window.requestAnimationFrame((...args) => resolve(callback(...args))));
}

export function debounce<T = void>(callback: (...args: any[]) => T, wait: number, controller?: AbortController): (...args: any[]) => void {
  let timeoutId: number;
  controller?.signal.addEventListener('abort', () => window.clearTimeout(timeoutId), {
    once: true,
    passive: true
  });

  return (...args) => {
    const invoke = () => {
      window.clearTimeout(timeoutId);
      return callback(...args);
    };

    window.clearTimeout(timeoutId);
    timeoutId = window.setTimeout(invoke, wait);
  };
}