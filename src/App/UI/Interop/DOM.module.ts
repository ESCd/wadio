export function addBreakpointListener(callback: InteropCallback<BreakpointChangeEvent>) {
  const state = { active: getActiveBreakpoint() };
  const handler = () => {
    const active = getActiveBreakpoint();
    if (state.active !== active) {
      return callback.invokeMethodAsync('Invoke', {
        from: state.active,
        to: state.active = active
      });
    }
  };

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
      document.body.removeEventListener('click', handler, { capture: true });
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

  return DOMBreakpoint.ExtraSmall;
};

const isHitTarget = (target: HTMLElement, element: HTMLElement) => {
  if (target === element) return true;

  while (target.parentElement && (target = target.parentElement)) {
    if (target === element) return true;
  }

  return false;
};

enum DOMBreakpoint {
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

type InteropCallback<T = void> = {
  invokeMethodAsync(methodName: 'Invoke', arg?: T): Promise<void>;
}