// Lightweight password strength heuristic — no external dependency.
// Returns { score: 0..4, label, color } based on length and character variety.

export function evaluatePasswordStrength(password) {
  const pw = password || '';
  let score = 0;

  if (pw.length >= 8) score += 1;
  if (pw.length >= 12) score += 1;
  if (/[A-Z]/.test(pw) && /[a-z]/.test(pw)) score += 1;
  if (/[0-9]/.test(pw)) score += 1;
  if (/[^A-Za-z0-9]/.test(pw)) score += 1;

  // Cap at 4 buckets for display.
  score = Math.min(score, 4);

  const buckets = [
    { label: 'Too weak', color: '#e0564f' },
    { label: 'Weak', color: '#e0894f' },
    { label: 'Fair', color: '#d8b84a' },
    { label: 'Good', color: '#6aa84f' },
    { label: 'Strong', color: '#4caf50' },
  ];

  return { score, ...buckets[score] };
}

// Mirrors the backend rule: min 8 chars, at least one letter and one number.
export function meetsPasswordPolicy(password) {
  const pw = password || '';
  return pw.length >= 8 && /[A-Za-z]/.test(pw) && /[0-9]/.test(pw);
}
