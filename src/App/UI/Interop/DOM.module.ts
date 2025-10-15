export function addAppInstalledListener(callback: InteropCallback) {
  const handler = () => {
    return callback.invokeMethodAsync('Invoke');
  };

  window.addEventListener('appinstalled', handler, {
    capture: true,
    passive: true,
  });

  return {
    dispose() {
      window.removeEventListener('appinstalled', handler);
    }
  };
};

export function addClickOutListener(element: HTMLElement) {
  const handler = (e: MouseEvent) => {
    if (!isHitTarget(e.target as HTMLElement, element)) {
      document.body.removeEventListener('click', handler);
      element.dispatchEvent(new CustomEvent('clickout', {
        bubbles: true,
        detail: e
      }));
    }
  };

  document.body.addEventListener('click', handler, {
    capture: true,
    passive: true
  });

  return {
    dispose() {
      document.body.removeEventListener('click', handler);
    }
  };
};

export function addFullscreenChangeListener(callback: InteropCallback) {
  const state = { value: isFullscreen() };
  const handler = () => {
    const value = isFullscreen();
    if (state.value !== value) {
      state.value = value;
      return callback.invokeMethodAsync('Invoke');
    }
  }

  document.addEventListener('fullscreenchange', handler, {
    capture: true,
    passive: true
  });

  window.addEventListener('resize', handler, {
    capture: true,
    passive: true
  });

  return {
    dispose() {
      window.removeEventListener('resize', handler);
      document.removeEventListener('fullscreenchange', handler);
    }
  };
};

export function isApplicationInstalled() {
  if ('standalone' in navigator && navigator.standalone) {
    return true;
  }

  return window.matchMedia(`(display-mode: standalone)`).matches
};

export function isFullscreen() {
  if (!document.fullscreenEnabled) {
    return false;
  }

  if (!!document.fullscreenElement) {
    return true;
  }

  return (screen.width === window.innerWidth || screen.width === window.outerWidth)
    && (screen.height === window.innerHeight || screen.height === window.outerHeight);
};

const isHitTarget = (target: HTMLElement, element: HTMLElement) => {
  if (target === element) return true;

  while (target.parentElement && (target = target.parentElement)) {
    if (target === element) return true;
  }

  return false;
};

type InteropCallback = {
  invokeMethodAsync(methodName: 'Invoke'): Promise<void>;
}