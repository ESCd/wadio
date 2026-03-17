import { animate, debounce, isDescendantOf } from './core';

const observer = new ResizeObserver(async entries => await Promise.all(entries.map(entry => animate(() => {
  const element = entry.target as HTMLDivElement;
  if (!element) return;

  const size = {
    height: element.offsetHeight,
    width: element.offsetWidth
  };

  return element.dispatchEvent(new CustomEvent('resize', {
    bubbles: true,
    detail: { ...size }
  }));
}))));

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

export function addBreakpointListener(callback: InteropCallback<BreakpointChangeEvent>) {
  const state = { active: getActiveBreakpoint() };
  const handler = () => animate(() => {
    const active = getActiveBreakpoint();
    if (state.active !== active) {
      return callback.invokeMethodAsync('Invoke', {
        from: state.active,
        to: state.active = active
      });
    }
  });

  window.addEventListener('resize', handler, {
    capture: true,
    passive: true
  });

  return {
    dispose() {
      window.removeEventListener('resize', handler, { capture: true });
    }
  };
};

export function addClickOutListener(element: HTMLElement) {
  const handler = (e: MouseEvent) => {
    if (!element) return;

    if (!isDescendantOf(e.target as HTMLElement, element)) {
      return element.dispatchEvent(new CustomEvent('clickout', {
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
      document.body.removeEventListener('click', handler, { capture: true });
    }
  };
};

export function addFullscreenChangeListener(callback: InteropCallback) {
  const state = { value: isFullscreen() };
  const handler = debounce(() => animate(() => {
    const value = isFullscreen();
    if (state.value !== value) {
      state.value = value;
      return callback.invokeMethodAsync('Invoke');
    }
  }), 125);

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
      window.removeEventListener('resize', handler, { capture: true });
      document.removeEventListener('fullscreenchange', handler, { capture: true });
    }
  };
};

export function addResizeObserver(element: HTMLElement) {
  const handler = debounce((e: CustomEvent) => element.dispatchEvent(new CustomEvent('resizedebounce', {
    bubbles: true,
    detail: e.detail,
  })), 75);

  element.addEventListener('resize', handler, { capture: true });

  observer.observe(element);
  return {
    dispose() {
      observer.unobserve(element);
      element?.removeEventListener('resize', handler, { capture: true });
    }
  };
};

export function getActiveBreakpoint() {
  if (window.matchMedia('(min-width: 1536px) or (width >= 96rem)').matches) {
    return DOMBreakpoint.ExtraExtraLarge;
  }

  if (window.matchMedia('(min-width: 1280px) or (width >= 80rem)').matches) {
    return DOMBreakpoint.ExtraLarge;
  }

  if (window.matchMedia('(min-width: 1024px) or (width >= 64rem)').matches) {
    return DOMBreakpoint.Large;
  }

  if (window.matchMedia('(min-width: 768px) or (width >= 48rem)').matches) {
    return DOMBreakpoint.Medium;
  }

  if (window.matchMedia('(min-width: 640px) or (width >= 40rem)').matches) {
    return DOMBreakpoint.Small;
  }

  if (window.matchMedia('(min-width: 360px) or (width >= 22.5rem)').matches) {
    return DOMBreakpoint.ExtraSmall;
  }

  return DOMBreakpoint.ExtraExtraSmall;
};

export function isApplicationInstalled() {
  if ('standalone' in navigator && navigator.standalone) {
    return true;
  }

  return window.matchMedia(`(display-mode: standalone)`).matches;
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

enum DOMBreakpoint {
  ExtraExtraSmall,
  ExtraSmall,
  Small,
  Medium,
  Large,
  ExtraLarge,
  ExtraExtraLarge,
};

type BreakpointChangeEvent = {
  from: DOMBreakpoint;
  to: DOMBreakpoint;
};