// TypeScript 4.9's lib.esnext.intl.d.ts doesn't include Intl.supportedValuesOf yet
// (added to the TS lib defs in 5.1) even though it's a standard, widely-supported
// runtime API (ECMA-402) - this augments the ambient Intl namespace with just its
// signature so the codebase can use it without bumping TypeScript.
declare namespace Intl {
    function supportedValuesOf(key: string): string[];
}
