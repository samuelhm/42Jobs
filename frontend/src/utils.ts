export function formatDescription(text: string | null): string {
  if (!text) return '';

  let result = text;

  const cutOff = result.indexOf('Show more Show less');
  if (cutOff !== -1) {
    result = result.substring(0, cutOff).trim();
  }

  result = result.replace(/•/g, '\n•');
  result = result.replace(/([.!?])\s+([A-Z])/g, '$1\n$2');

  return result.trim();
}
