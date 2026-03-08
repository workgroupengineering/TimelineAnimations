# Flash FLA Support Analysis

## Goal

Add robust Adobe Animate / Flash `FLA` support for:

- open
- save
- edit after load
- convert between `FLA` and `XFL`
- clear user-facing diagnostics about what is and is not supported

## Grounded findings

### 1. Modern `FLA` is an XFL package problem

Adobe documents current Animate authoring interchange around `XFL`, and describes:

- compressed `FLA`
- uncompressed `XFL`
- shared XML authoring structure

That means modern `FLA` support is primarily:

- package detection
- archive read/write
- folder read/write
- XFL DOM fidelity

not a separate proprietary semantic model.

### 2. Legacy binary `FLA` is a different format family

Pre-CS5 / pre-XFL `FLA` is the older binary format. Adobe’s public XFL documentation does
not provide a public, complete specification for legacy binary FLA authoring internals.

Implication:

- modern Animate `FLA` can be supported to a high degree through `XFL` parity
- legacy binary `FLA` cannot honestly be claimed as `100% parity` from public docs alone

### 3. Existing app state

The app already has substantial `FlashXflExchangeService` support:

- XML XFL import/export
- packaged archive import/export through `DOMDocument.xml`
- `LIBRARY/*.xml`, `LIBRARY/manifest.xml`, `PublishSettings.xml`
- file-type detection that treats `.fla` as Flash/XFL

Current gaps:

- `.xfl` currently behaves mostly like a single XML document in the generic file workflow
  instead of a true uncompressed XFL package/folder workflow
- no dedicated reusable package/container library project
- no explicit user workflow for:
  - open XFL folder
  - save XFL folder
  - convert FLA archive to XFL folder
  - convert XFL folder to FLA archive
- no explicit legacy-binary FLA detection and user-facing failure mode

### 4. Permissive OSS library audit

I did not find a practical permissive library that covers editable modern Animate `FLA` /
`XFL` authoring end to end.

Relevant candidates and why they do not satisfy the requirement:

| Candidate | License | Why it is not sufficient |
| --- | --- | --- |
| `openfl/swf` | MIT | SWF runtime/parser focus, not FLA/XFL authoring packages |
| `flxanimate` | MIT | Runtime playback for Animate exports, not authoring file read/write |
| `jindrapetrik/flacomdoc` | MIT | Documentation tooling around Animate content, not full authoring package IO |
| `jindrapetrik/flash-jsfl-collection` | MIT | JSFL automation scripts that require Adobe Animate, not a standalone FLA/XFL library |
| `jindrapetrik/jpexs-decompiler` | GPL-3.0 | Not permissive; focused on SWF decompilation, not editable FLA authoring parity |

Conclusion:

- no practical permissive drop-in library exists for this app’s requirement
- we should build a dedicated internal library for modern `FLA` / `XFL` package handling
- legacy binary `FLA` should be explicitly detected and reported as unsupported

## Implementation boundary

### What can be implemented with high confidence now

- modern compressed `FLA` archive IO
- modern uncompressed `XFL` folder IO
- `FLA <-> XFL` conversion
- editor open/save/edit workflow for those formats
- stronger diagnostics and compatibility reporting

### What cannot be honestly claimed from public information

- full parity for legacy binary Macromedia / pre-XFL `FLA`

For that format family, the correct product behavior is:

- detect likely legacy binary `FLA`
- stop import
- report the limitation clearly
- instruct the user to re-save in Adobe Animate as modern `FLA`/`XFL`

## References

- Adobe Animate product page: <https://www.adobe.com/pl/products/animate.html>
- Adobe Animate workspace and authoring docs: <https://helpx.adobe.com/pl/animate/using/workflow-workspace.html>
- Adobe XFL / Extensible Markup Language file docs: <https://helpx.adobe.com/animate/using/extensible-markup-language-file.html>
- `openfl/swf`: <https://github.com/openfl/swf>
- `flxanimate`: <https://github.com/scenee/FlxAnimate>
- `jindrapetrik/flacomdoc`: <https://github.com/jindrapetrik/flacomdoc>
- `jindrapetrik/flash-jsfl-collection`: <https://github.com/jindrapetrik/flash-jsfl-collection>
- `jindrapetrik/jpexs-decompiler`: <https://github.com/jindrapetrik/jpexs-decompiler>
