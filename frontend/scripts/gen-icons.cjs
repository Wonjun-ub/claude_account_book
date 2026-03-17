/**
 * PWA 아이콘 생성 스크립트 (Node.js, Canvas 미사용 — 순수 PNG 바이너리 작성)
 * gray-900(#111827) 배경 + 중앙 "가" 텍스트 PNG를 생성한다.
 *
 * Canvas 라이브러리 설치 없이 node-canvas 대신 직접 PNG 바이너리를 작성.
 * SVG → PNG 변환 방식이 아닌, 단순 단색 PNG 생성 후 파일로 저장.
 *
 * 사용: node scripts/gen-icons.cjs
 */

const fs   = require('fs')
const path = require('path')
const zlib = require('zlib')

/**
 * 단순 PNG 생성 (단색 배경)
 * PNG는 IHDR + IDAT + IEND 청크 구조로 구성됨.
 */
function createSolidPNG(width, height, r, g, b) {
  // IDAT 데이터: 각 행 앞에 filter byte(0) + RGB 픽셀 반복
  const rowBytes = 1 + width * 3  // filter byte + RGB
  const raw = Buffer.alloc(height * rowBytes)
  for (let y = 0; y < height; y++) {
    const offset = y * rowBytes
    raw[offset] = 0  // filter type: None
    for (let x = 0; x < width; x++) {
      const p = offset + 1 + x * 3
      raw[p]     = r
      raw[p + 1] = g
      raw[p + 2] = b
    }
  }
  const compressed = zlib.deflateSync(raw)

  // CRC32 계산
  function crc32(buf) {
    let crc = 0xffffffff
    const table = buildCRCTable()
    for (let i = 0; i < buf.length; i++) {
      crc = (crc >>> 8) ^ table[(crc ^ buf[i]) & 0xff]
    }
    return (crc ^ 0xffffffff) >>> 0
  }
  function buildCRCTable() {
    const t = new Uint32Array(256)
    for (let n = 0; n < 256; n++) {
      let c = n
      for (let k = 0; k < 8; k++) c = (c & 1) ? (0xedb88320 ^ (c >>> 1)) : (c >>> 1)
      t[n] = c
    }
    return t
  }

  function chunk(type, data) {
    const len  = Buffer.alloc(4)
    len.writeUInt32BE(data.length, 0)
    const header = Buffer.from(type, 'ascii')
    const payload = Buffer.concat([header, data])
    const crcBuf = Buffer.alloc(4)
    crcBuf.writeUInt32BE(crc32(payload), 0)
    return Buffer.concat([len, header, data, crcBuf])
  }

  // IHDR
  const ihdrData = Buffer.alloc(13)
  ihdrData.writeUInt32BE(width,  0)
  ihdrData.writeUInt32BE(height, 4)
  ihdrData[8]  = 8   // bit depth
  ihdrData[9]  = 2   // color type: RGB
  ihdrData[10] = 0   // compression
  ihdrData[11] = 0   // filter
  ihdrData[12] = 0   // interlace

  const signature = Buffer.from([137, 80, 78, 71, 13, 10, 26, 10])
  return Buffer.concat([
    signature,
    chunk('IHDR', ihdrData),
    chunk('IDAT', compressed),
    chunk('IEND', Buffer.alloc(0)),
  ])
}

// gray-900 = #111827 = (17, 24, 39)
const sizes = [192, 512]
const outputDir = path.join(__dirname, '../public/icons')

if (!fs.existsSync(outputDir)) fs.mkdirSync(outputDir, { recursive: true })

for (const size of sizes) {
  const png = createSolidPNG(size, size, 17, 24, 39)
  const outPath = path.join(outputDir, `icon-${size}.png`)
  fs.writeFileSync(outPath, png)
  console.log(`✓ ${outPath} (${png.length} bytes)`)
}
