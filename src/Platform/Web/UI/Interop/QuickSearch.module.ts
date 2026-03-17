export function useKeyboardNavigation(container: HTMLElement, listing: HTMLElement) {
  const handler = (e: KeyboardEvent): void => {
    if (!listing?.children?.length) return;

    const getNextElement = () => {
      if (document.activeElement?.parentElement?.parentElement === listing) {
        for (const child of listing.children) {
          if (document.activeElement == child.firstElementChild) {
            return child.nextElementSibling?.firstElementChild as HTMLElement;
          }
        }
      }

      return listing.children[0].firstElementChild as HTMLElement;
    };

    const getPrevElement = () => {
      if (document.activeElement?.parentElement?.parentElement === listing) {
        for (const child of listing.children) {
          if (document.activeElement == child.firstElementChild) {
            return child.previousElementSibling?.firstElementChild as HTMLElement;
          }
        }
      }

      return listing.children[listing.children.length - 1].firstElementChild as HTMLElement;
    };

    if (e.code === 'ArrowDown') {
      const next = getNextElement();
      if (next) {
        e.preventDefault();
        return next.focus();
      }
    }

    if (e.code === 'ArrowUp') {
      const prev = getPrevElement();
      if (prev) {
        e.preventDefault();
        return prev.focus();
      }
    }
  };

  container.addEventListener('keydown', handler, { capture: true });
  return {
    dispose() {
      container?.removeEventListener('keydown', handler, { capture: true });
    }
  }
};