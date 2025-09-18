const observer = new ResizeObserver(([{ borderBoxSize: [{ blockSize: height, inlineSize: width }], target }]) => {
  const size = { height, width }
  target.dispatchEvent(new CustomEvent('marqueeresize', {
    bubbles: true,
    detail: { ...size }
  }))
});

export function attach(target: HTMLElement, parent: HTMLElement) {
  observer.observe(target);
  observer.observe(parent);

  return {
    dispose() {
      observer.unobserve(parent);
      observer.unobserve(target);
    },

    measure() {
      const innerWidth = target.scrollWidth;
      const outerWidth = parent.clientWidth;
      return {
        isOverflowing: innerWidth > outerWidth,
        innerWidth,
        outerWidth
      };
    }
  };
};