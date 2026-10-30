const MAX_SIDE = 1280

/**
 * Downsizes a phone photo to a JPEG with its longest side at most 1280 px (about 200 KB), so it fits the
 * upload limit and slow networks. Without canvas support it keeps the original; the server still checks size.
 */
export async function resizePhoto(file: Blob): Promise<Blob> {
  if (typeof createImageBitmap !== 'function') {
    return file
  }
  try {
    const bitmap = await createImageBitmap(file)
    const scale = Math.min(1, MAX_SIDE / Math.max(bitmap.width, bitmap.height))
    const canvas = document.createElement('canvas')
    canvas.width = Math.round(bitmap.width * scale)
    canvas.height = Math.round(bitmap.height * scale)
    const context = canvas.getContext('2d')
    if (!context) {
      return file
    }
    context.drawImage(bitmap, 0, 0, canvas.width, canvas.height)
    bitmap.close()
    const jpeg = await new Promise<Blob | null>((resolve) => canvas.toBlob(resolve, 'image/jpeg', 0.8))
    return jpeg ?? file
  } catch {
    return file
  }
}
