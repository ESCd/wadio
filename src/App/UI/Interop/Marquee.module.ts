import mobile from 'is-mobile';

const observer = new ResizeObserver(([{ borderBoxSize: [{ blockSize: height, inlineSize: width }], target }]) => {
  if (!target) return;

  const size = { height, width }
  target.dispatchEvent(new CustomEvent('marqueeresize', {
    bubbles: true,
    detail: { ...size }
  }))
});

export function attach(target: HTMLElement, parent: HTMLElement) {
  observer.observe(target);
  observer.observe(parent);

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
      observer.unobserve(parent);
      observer.unobserve(target);

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