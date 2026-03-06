# HTML CSS Keyframes Interop Plan

## Objective

Support a web-friendly interchange format using embedded CSS keyframes and native HTML elements where practical.

## File Shape

- XHTML document
- embedded `<style>` block
- stage container
- absolutely positioned child elements
- `@keyframes` per exported layer

## Supported HTML Surface

- `div`
- `button`
- `input type="text"`
- `input type="checkbox"`
- `input type="range"`
- `progress`
- `img`
- inline `svg` for vector path layers when needed

## Import Strategy

1. Parse XHTML with `XDocument`.
2. Read stage container dimensions and background.
3. Recover layer/control metadata from `data-*` attributes.
4. Parse embedded CSS for base rules and `@keyframes`.
5. Convert CSS keyframes to internal property tracks.

## Export Strategy

1. Export active scene as XHTML with embedded CSS.
2. Use native HTML controls for control layers.
3. Use baked transform and property samples.
4. Embed metadata so import can recover exact layer/control intent.

## Acceptance

- exported XHTML loads in a browser without external assets
- supported layers can be re-imported back into the editor
- native control layers remain native HTML controls in export
