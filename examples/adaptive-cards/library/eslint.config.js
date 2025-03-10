import { defineConfig, globalIgnores } from "eslint/config";
import globals from "globals";
import tseslint from "typescript-eslint";


export default defineConfig([
  { files: ["**/*.{ts}"] },
  { files: ["**/*.{ts}"], languageOptions: { globals: globals.browser } },
  tseslint.configs.recommended,
  globalIgnores(["node_modules/","dist/*"])
]);