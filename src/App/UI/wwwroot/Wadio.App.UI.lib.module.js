export function afterStarted(blazor) {
  blazor.registerCustomEventType('clickout', { createEventArgs: e => e.detail });
  blazor.registerCustomEventType('resize', { createEventArgs: e => e.detail });
  blazor.registerCustomEventType('resizedebounce', { createEventArgs: e => e.detail });
};