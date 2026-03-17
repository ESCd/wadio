import hotkeys from 'hotkeys-js';

export function addHotKeyListener(hotkey: string, scope: string, callback: InteropCallback) {
  const handler = (e: KeyboardEvent, { }): void => {
    e.preventDefault();
    callback.invokeMethodAsync('Invoke');
  };

  if (scope?.length) {
    hotkeys(hotkey, scope, handler);
  } else {
    hotkeys(hotkey, handler);
  }

  return {
    dispose() {
      if (scope?.length) {
        return hotkeys.unbind(hotkey, scope, handler);
      }

      return hotkeys.unbind(hotkey, handler);
    }
  };
};