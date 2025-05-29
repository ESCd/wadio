const isHitTarget = (target: HTMLElement, element: HTMLElement) => {
  if (target === element) return true;

  while (target.parentElement && (target = target.parentElement)) {
    if (target === element) return true;
  }

  return false;
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

  document.body.addEventListener('click', handler, { capture: true, passive: true });
  return {
    dispose() {
      document.body.removeEventListener('click', handler);
    }
  };
};