// Node 16에서 Vite가 node:crypto의 getRandomValues를 호출하는 이슈 패치
const nodeCrypto = require('crypto')
if (!nodeCrypto.getRandomValues) {
  nodeCrypto.getRandomValues = function(buf) {
    return nodeCrypto.webcrypto.getRandomValues(buf)
  }
}
if (!global.crypto) {
  global.crypto = nodeCrypto.webcrypto
}
