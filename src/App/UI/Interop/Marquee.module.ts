import mobile from 'is-mobile';

export function attach(target: HTMLElement, parent: HTMLElement) {
  const handler = (e: PointerEvent) => {
    e.stopPropagation();
    e.preventDefault();
  };

  if (mobile()) {
    parent.addEventListener('contextmenu', handler, {
      passive: false,
    });
  }

  return {
    dispose() {
      parent.removeEventListener('contextmenu', handler);
    },

    measure() {
      const innerWidth = target.scrollWidth;
      const outerWidth = parent.clientWidth;

      return {
        overflowing: innerWidth > outerWidth,
        innerWidth,
        outerWidth
      };
    }
  };
};