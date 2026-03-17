export function write(text: string): Promise<void> {
  return navigator.clipboard.writeText(text);
}