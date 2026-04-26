export function getErrorMessage(error, fallback = 'Operation failed') {
  const responseData = error?.response?.data;

  if (typeof responseData === 'string' && responseData.trim()) {
    return responseData;
  }

  if (responseData?.error) {
    return String(responseData.error);
  }

  if (responseData?.Error) {
    return String(responseData.Error);
  }

  if (error?.message) {
    return String(error.message);
  }

  return fallback;
}
