export function canNavigate() {
  return {
    backward: window.navigation?.canGoBack ?? false,
    forward: window.navigation?.canGoForward ?? false,
  };
};

export async function back() {
  if ('navigation' in window) {
    return await window.navigation.back().finished;
  }

  return new Promise<void>(resolve => {
    const handler = () => {
      window.removeEventListener('popstate', handler);
      return resolve();
    };

    window.addEventListener('popstate', handler, {
      capture: true,
      once: true,
      passive: true,
    });

    return history.back();
  });
};

export async function forward() {
  if ('navigation' in window) {
    return await window.navigation.forward().finished;
  }

  return new Promise<void>(resolve => {
    const handler = () => {
      window.removeEventListener('popstate', handler);
      return resolve();
    };

    window.addEventListener('popstate', handler, {
      capture: true,
      once: true,
      passive: true,
    });

    return history.forward();
  });
};

export function isNavigationApiSupported() {
  return 'navigation' in window;
};