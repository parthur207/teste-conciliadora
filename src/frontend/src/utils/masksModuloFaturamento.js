export function maskCompetencia(value) {
  const digits = value.replace(/\D/g, '').slice(0, 6);

  if (digits.length <= 4) {
    return digits;
  }

  const ano = digits.slice(0, 4);
  const mes = digits.slice(4, 6);

  return `${ano}-${mes}`;
}