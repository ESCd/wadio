export function afterStarted(blazor) {
  blazor.registerCustomEventType('marqueeresize', { createEventArgs: e => e.detail });
};