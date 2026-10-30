# Changelog

## [1.9.0](https://github.com/ZeroAlloc-Net/ZeroAlloc.TestHelpers/compare/v1.8.2...v1.9.0) (2026-10-30)


### Features

* add MeasureBytesPerCallInterleavedAsync for figures compared with each other ([#101](https://github.com/ZeroAlloc-Net/ZeroAlloc.TestHelpers/issues/101)) ([9a5907a](https://github.com/ZeroAlloc-Net/ZeroAlloc.TestHelpers/commit/9a5907af83da80294fbf2e6f92078f28e81a0e16))


### Bug Fixes

* gate MeasureBytesPerCallInterleavedAsync on the path most calls take ([#103](https://github.com/ZeroAlloc-Net/ZeroAlloc.TestHelpers/issues/103)) ([a69e9d5](https://github.com/ZeroAlloc-Net/ZeroAlloc.TestHelpers/commit/a69e9d527b36d9da30e355ca88c0c500d6d47486))

## [1.8.2](https://github.com/ZeroAlloc-Net/ZeroAlloc.TestHelpers/compare/v1.8.1...v1.8.2) (2026-10-30)


### Bug Fixes

* measure AllocationGate in steady state and compare equal calls fairly ([#99](https://github.com/ZeroAlloc-Net/ZeroAlloc.TestHelpers/issues/99)) ([fa55208](https://github.com/ZeroAlloc-Net/ZeroAlloc.TestHelpers/commit/fa55208597f54884050ea80bd6cc2557f779c991)), closes [#94](https://github.com/ZeroAlloc-Net/ZeroAlloc.TestHelpers/issues/94) [#76](https://github.com/ZeroAlloc-Net/ZeroAlloc.TestHelpers/issues/76)

## [1.8.1](https://github.com/ZeroAlloc-Net/ZeroAlloc.TestHelpers/compare/v1.8.0...v1.8.1) (2026-10-29)


### Bug Fixes

* fail GeneratorSnapshot.Verify when the generator emits nothing ([#97](https://github.com/ZeroAlloc-Net/ZeroAlloc.TestHelpers/issues/97)) ([485aabc](https://github.com/ZeroAlloc-Net/ZeroAlloc.TestHelpers/commit/485aabcb45a6944f5bff57934eea03c542ff7556)), closes [#95](https://github.com/ZeroAlloc-Net/ZeroAlloc.TestHelpers/issues/95)

## [1.8.0](https://github.com/ZeroAlloc-Net/ZeroAlloc.TestHelpers/compare/v1.7.3...v1.8.0) (2026-10-17)


### Features

* measure allocation gates until the measurement is stable ([#90](https://github.com/ZeroAlloc-Net/ZeroAlloc.TestHelpers/issues/90)) ([d1810a6](https://github.com/ZeroAlloc-Net/ZeroAlloc.TestHelpers/commit/d1810a6eb619099327d147b5729dd902a5fb3293)), closes [#89](https://github.com/ZeroAlloc-Net/ZeroAlloc.TestHelpers/issues/89)

## [1.7.3](https://github.com/ZeroAlloc-Net/ZeroAlloc.TestHelpers/compare/v1.7.2...v1.7.3) (2026-10-16)


### Bug Fixes

* measure the sync allocation gate over several windows after a longer warmup ([#87](https://github.com/ZeroAlloc-Net/ZeroAlloc.TestHelpers/issues/87)) ([223448e](https://github.com/ZeroAlloc-Net/ZeroAlloc.TestHelpers/commit/223448eddd3cc1a8c7cf69018346ab8528f14346)), closes [#84](https://github.com/ZeroAlloc-Net/ZeroAlloc.TestHelpers/issues/84)

## [1.7.2](https://github.com/ZeroAlloc-Net/ZeroAlloc.TestHelpers/compare/v1.7.1...v1.7.2) (2026-10-16)


### Documentation

* rewrite the readme as a product page ([#80](https://github.com/ZeroAlloc-Net/ZeroAlloc.TestHelpers/issues/80)) ([86633cf](https://github.com/ZeroAlloc-Net/ZeroAlloc.TestHelpers/commit/86633cf618e8b8ab460746a51839799663bdf0be))

## [1.7.1](https://github.com/ZeroAlloc-Net/ZeroAlloc.TestHelpers/compare/v1.7.0...v1.7.1) (2026-10-15)


### Documentation

* add the standard badges and complete the async gate sample ([#77](https://github.com/ZeroAlloc-Net/ZeroAlloc.TestHelpers/issues/77)) ([61b00ce](https://github.com/ZeroAlloc-Net/ZeroAlloc.TestHelpers/commit/61b00ce325b1a6299c9b86fe2095cb192c8e1aeb))

## [1.7.0](https://github.com/ZeroAlloc-Net/ZeroAlloc.TestHelpers/compare/v1.6.2...v1.7.0) (2026-10-12)


### Features

* add AllocationGate methods for calls that complete asynchronously ([#73](https://github.com/ZeroAlloc-Net/ZeroAlloc.TestHelpers/issues/73)) ([d9fc1e9](https://github.com/ZeroAlloc-Net/ZeroAlloc.TestHelpers/commit/d9fc1e902f1c249cfe672591565be4a1afc67ebe)), closes [#65](https://github.com/ZeroAlloc-Net/ZeroAlloc.TestHelpers/issues/65)

## [1.6.2](https://github.com/ZeroAlloc-Net/ZeroAlloc.TestHelpers/compare/v1.6.1...v1.6.2) (2026-10-12)


### Refactors

* drop the HLQ012 and HLQ013 loop-shape comment ([#71](https://github.com/ZeroAlloc-Net/ZeroAlloc.TestHelpers/issues/71)) ([04eec68](https://github.com/ZeroAlloc-Net/ZeroAlloc.TestHelpers/commit/04eec685de7ba3e82f2bdca25ef7b77e9969bf35))

## [1.6.1](https://github.com/ZeroAlloc-Net/ZeroAlloc.TestHelpers/compare/v1.6.0...v1.6.1) (2026-10-11)


### Bug Fixes

* make AllocationGate.cs compile without ImplicitUsings ([#69](https://github.com/ZeroAlloc-Net/ZeroAlloc.TestHelpers/issues/69)) ([7431a66](https://github.com/ZeroAlloc-Net/ZeroAlloc.TestHelpers/commit/7431a668c6541fe0adb759638b3714f6257c4d5b))

## [1.6.0](https://github.com/ZeroAlloc-Net/ZeroAlloc.TestHelpers/compare/v1.5.1...v1.6.0) (2026-10-10)


### Features

* declare ZeroAlloc.TestHelpers as not AOT-compatible ([#66](https://github.com/ZeroAlloc-Net/ZeroAlloc.TestHelpers/issues/66)) ([dd9f6b8](https://github.com/ZeroAlloc-Net/ZeroAlloc.TestHelpers/commit/dd9f6b8e64214fb01c7b19a4b91ae2d4f7827db7))

## [1.5.1](https://github.com/ZeroAlloc-Net/ZeroAlloc.TestHelpers/compare/v1.5.0...v1.5.1) (2026-10-07)


### Bug Fixes

* settle the heap before the AllocationGate warmup, not after it ([#63](https://github.com/ZeroAlloc-Net/ZeroAlloc.TestHelpers/issues/63)) ([425eb17](https://github.com/ZeroAlloc-Net/ZeroAlloc.TestHelpers/commit/425eb17ac46f575f27d7a6ca7a36936a687ddc01)), closes [#62](https://github.com/ZeroAlloc-Net/ZeroAlloc.TestHelpers/issues/62)

## [1.5.0](https://github.com/ZeroAlloc-Net/ZeroAlloc.TestHelpers/compare/v1.4.0...v1.5.0) (2026-10-02)


### Features

* add TextSnapshot.VerifyText, which compiles without a Roslyn reference ([#60](https://github.com/ZeroAlloc-Net/ZeroAlloc.TestHelpers/issues/60)) ([0828ed7](https://github.com/ZeroAlloc-Net/ZeroAlloc.TestHelpers/commit/0828ed71200d5541ec42da7bd6144215521fbf36)), closes [#59](https://github.com/ZeroAlloc-Net/ZeroAlloc.TestHelpers/issues/59)

## [1.4.0](https://github.com/ZeroAlloc-Net/ZeroAlloc.TestHelpers/compare/v1.3.3...v1.4.0) (2026-10-01)


### Features

* expose AllocationGate's per-call measurement and add relative gates ([#57](https://github.com/ZeroAlloc-Net/ZeroAlloc.TestHelpers/issues/57)) ([00f5f01](https://github.com/ZeroAlloc-Net/ZeroAlloc.TestHelpers/commit/00f5f012cf00bafd22103dbd1df6328b553e519b)), closes [#56](https://github.com/ZeroAlloc-Net/ZeroAlloc.TestHelpers/issues/56)

## [1.3.3](https://github.com/ZeroAlloc-Net/ZeroAlloc.TestHelpers/compare/v1.3.2...v1.3.3) (2026-09-24)


### Bug Fixes

* compile GeneratorSnapshot only in consumers that reference Roslyn ([#51](https://github.com/ZeroAlloc-Net/ZeroAlloc.TestHelpers/issues/51)) ([f87ac53](https://github.com/ZeroAlloc-Net/ZeroAlloc.TestHelpers/commit/f87ac53cbb83b88a3b93b24e91eacf80566bd9fc)), closes [#50](https://github.com/ZeroAlloc-Net/ZeroAlloc.TestHelpers/issues/50)

## [1.3.2](https://github.com/ZeroAlloc-Net/ZeroAlloc.TestHelpers/compare/v1.3.1...v1.3.2) (2026-09-20)


### Bug Fixes

* **ci:** pin the SDK floor at the .NET 10 GA band, not the newest patch ([#45](https://github.com/ZeroAlloc-Net/ZeroAlloc.TestHelpers/issues/45)) ([d9660f0](https://github.com/ZeroAlloc-Net/ZeroAlloc.TestHelpers/commit/d9660f0bd1f257f0cfd749baece2e8d6cdb80a9d))

## [1.3.1](https://github.com/ZeroAlloc-Net/ZeroAlloc.TestHelpers/compare/v1.3.0...v1.3.1) (2026-09-20)


### Bug Fixes

* satisfy CA1512 in AllocationGate ([#43](https://github.com/ZeroAlloc-Net/ZeroAlloc.TestHelpers/issues/43)) ([370b7df](https://github.com/ZeroAlloc-Net/ZeroAlloc.TestHelpers/commit/370b7df240de2b81a83c2492ee34b21f68edb66e))

## [1.3.0](https://github.com/ZeroAlloc-Net/ZeroAlloc.TestHelpers/compare/v1.2.3...v1.3.0) (2026-09-20)


### Features

* accept a run result, and resolve snapshots from the project ([#41](https://github.com/ZeroAlloc-Net/ZeroAlloc.TestHelpers/issues/41)) ([13b950a](https://github.com/ZeroAlloc-Net/ZeroAlloc.TestHelpers/commit/13b950ada5f55895ed179e40f322bc88d61915fa))

## [1.2.3](https://github.com/ZeroAlloc-Net/ZeroAlloc.TestHelpers/compare/v1.2.2...v1.2.3) (2026-09-19)


### Bug Fixes

* survive deterministic source paths ([#39](https://github.com/ZeroAlloc-Net/ZeroAlloc.TestHelpers/issues/39)) ([4ddc290](https://github.com/ZeroAlloc-Net/ZeroAlloc.TestHelpers/commit/4ddc290a1e34c484aea3c0121b390fe012174df1))

## [1.2.2](https://github.com/ZeroAlloc-Net/ZeroAlloc.TestHelpers/compare/v1.2.1...v1.2.2) (2026-09-19)


### Bug Fixes

* make the helper analyzer-clean across consuming repos ([#37](https://github.com/ZeroAlloc-Net/ZeroAlloc.TestHelpers/issues/37)) ([980a712](https://github.com/ZeroAlloc-Net/ZeroAlloc.TestHelpers/commit/980a712749a18dfd45abae71400d0bb624534209))

## [1.2.1](https://github.com/ZeroAlloc-Net/ZeroAlloc.TestHelpers/compare/v1.2.0...v1.2.1) (2026-09-19)


### Bug Fixes

* resolve the real test method instead of trusting CallerMemberName ([#35](https://github.com/ZeroAlloc-Net/ZeroAlloc.TestHelpers/issues/35)) ([273549b](https://github.com/ZeroAlloc-Net/ZeroAlloc.TestHelpers/commit/273549bf2512b9801d7d3606be8dde55c4fac7cb))

## [1.2.0](https://github.com/ZeroAlloc-Net/ZeroAlloc.TestHelpers/compare/v1.1.0...v1.2.0) (2026-09-19)


### Features

* make the snapshot API uniform, and add a text overload ([#33](https://github.com/ZeroAlloc-Net/ZeroAlloc.TestHelpers/issues/33)) ([75a325f](https://github.com/ZeroAlloc-Net/ZeroAlloc.TestHelpers/commit/75a325f1b145f4ccfb92da2e6c6d867a44d6e48f))

## [1.1.0](https://github.com/ZeroAlloc-Net/ZeroAlloc.TestHelpers/compare/v1.0.0...v1.1.0) (2026-09-19)


### Features

* add GeneratorSnapshot, a Verify-free source-generator snapshot harness ([#30](https://github.com/ZeroAlloc-Net/ZeroAlloc.TestHelpers/issues/30)) ([44f9b75](https://github.com/ZeroAlloc-Net/ZeroAlloc.TestHelpers/commit/44f9b75d535036b9f9aa48128da9e69766fafebd))

## 1.0.0 (2026-05-22)


### Features

* AllocationGate source-distributed via contentFiles ([36f5e69](https://github.com/ZeroAlloc-Net/ZeroAlloc.TestHelpers/commit/36f5e69d750f551a095b1f55cca78ef66023715f))

## Changelog
