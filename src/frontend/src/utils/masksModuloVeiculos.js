export function maskPlaca(value) {
  return value
    .toUpperCase()
    .replace(/[^A-Z0-9]/g, '')
    .slice(0, 7);
}

export function maskAno(value) {
  return value
    .replace(/\D/g, '')
    .slice(0, 4);
}