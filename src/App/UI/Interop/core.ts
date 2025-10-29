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

export function isDescendantOf(target: Element, ancestor: Element) {
  if (!target || !ancestor) return false;
  if (target === ancestor) return true;

  while (target.parentElement && (target = target.parentElement)) {
    if (target === ancestor) return true;
  }

  return false;
}