export function maskValorMonetario(value) {
  const digits = value.replace(/\D/g, '');

  if (!digits)
    return '';

  const number = Number(digits) / 100;

  return number.toFixed(2);
}