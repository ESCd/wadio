const createEventArgs = e => e.detail;

export function afterStarted(blazor) {
  blazor.registerCustomEventType('clickout', { createEventArgs });
  blazor.registerCustomEventType('resize', { createEventArgs });
  blazor.registerCustomEventType('resizedebounce', { createEventArgs });
};