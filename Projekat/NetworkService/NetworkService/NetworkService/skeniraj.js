// collect-code.js
const fs = require("fs");
const path = require("path");

const targetDirs = ["Common", "Converters", "Model", "ViewModel", "Views"];
const exts = new Set([".cs", ".xaml"]);
const root = path.resolve(process.argv[2] || process.cwd());
const outputFile = path.join(root, "all_code.txt");

// (opciono) izbegni obj/bin
const SKIP_DIRS = new Set(["bin", "obj", ".git", ".vs", "node_modules"]);

function dirExists(p) {
  try { return fs.statSync(p).isDirectory(); } catch { return false; }
}

function* walk(dir) {
  const entries = fs.readdirSync(dir, { withFileTypes: true });
  for (const e of entries) {
    const full = path.join(dir, e.name);
    if (e.isDirectory()) {
      if (!SKIP_DIRS.has(e.name)) yield* walk(full);
    } else if (exts.has(path.extname(e.name))) {
      yield full;
    }
  }
}

function main() {
  const out = fs.createWriteStream(outputFile, { encoding: "utf8" });
  let found = 0;

  const roots = targetDirs
    .map(d => path.join(root, d))
    .filter(dirExists);

  if (roots.length === 0) {
    console.error("Nisam našao nijedan od foldera u:", root);
    process.exit(1);
  }

  for (const base of roots) {
    for (const file of walk(base)) {
      found++;
      const code = fs.readFileSync(file, "utf8");
      out.write("========================================\n");
      out.write(`FILE: ${file}\n`);
      out.write("========================================\n");
      out.write(code);
      out.write("\n\n");
    }
  }

  out.end(() => {
    console.log(`Gotovo. Nađeno fajlova: ${found}. Rezultat: ${outputFile}`);
    if (found === 0) {
      console.warn("Upozorenje: Nije nađen nijedan .cs/.xaml fajl. Provjeri root/folder imena.");
    }
  });
}

main();
