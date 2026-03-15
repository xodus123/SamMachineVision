"""
Pre-build rag_index.json with embeddings.
Run this once when data files change. The output is shipped with the app.

Usage:
    python build_rag_index.py

Requires: Ollama running with qwen3-embedding:0.6b model installed.
"""

import json, os, re, sys, requests

OLLAMA_URL = "http://localhost:11434"
EMBED_MODEL = "qwen3-embedding:0.6b"
DATA_DIR = os.path.join(os.path.dirname(__file__), "Data", "data")
OUTPUT_PATH = os.path.join(os.path.dirname(__file__), "Data", "rag_index.json")
MAX_CHUNK_LENGTH = 1500
OVERLAP_LENGTH = 200


def strip_markdown_decorations(text: str) -> str:
    """Remove markdown decorations to save tokens."""
    lines = text.split("\n")
    result = []
    for line in lines:
        # Table separator (| --- | --- |)
        if re.match(r"^\s*\|[\s\-:|\+]+\|\s*$", line):
            continue
        # Code block (```)
        if line.strip().startswith("```"):
            continue
        # Horizontal rule (---, ***)
        if re.match(r"^\s*[-\*_]{3,}\s*$", line):
            continue
        # Bold (**text**) -> text
        line = line.replace("**", "")
        # Table row: convert | separators to spaces
        stripped = line.strip()
        if stripped.startswith("|") and stripped.endswith("|"):
            cells = stripped.strip("|").split("|")
            line = "  ".join(c.strip() for c in cells if c.strip())
        result.append(line)
    return "\n".join(result).strip()


def split_long_text(text: str, max_len: int) -> list[str]:
    """Split text into parts at paragraph boundaries with overlap."""
    if len(text) <= max_len:
        return [text]

    paragraphs = [p.strip() for p in text.split("\n\n") if p.strip()]
    parts = []
    current = ""
    last_para = ""

    for para in paragraphs:
        # Force split if single paragraph exceeds max_len
        if len(para) > max_len:
            if current:
                parts.append(current.strip())
                current = ""
            for pos in range(0, len(para), max_len - OVERLAP_LENGTH):
                end = min(pos + max_len, len(para))
                parts.append(para[pos:end])
            last_para = ""
            continue

        if current and len(current) + len(para) + 2 > max_len:
            parts.append(current.strip())
            # Overlap: include last paragraph if short enough
            current = last_para + "\n" + para if last_para and len(last_para) <= OVERLAP_LENGTH else para
        else:
            current = current + "\n\n" + para if current else para

        last_para = para

    if current.strip():
        parts.append(current.strip())

    return parts if parts else [text[:max_len]]


def split_markdown_into_chunks(file_path: str, source: str) -> list[dict]:
    """Split markdown file by headings (##/###) into chunks."""
    with open(file_path, "r", encoding="utf-8") as f:
        content = f.read()

    file_name = os.path.splitext(os.path.basename(file_path))[0]
    heading_re = re.compile(r"^(#{2,4})\s+(.+)$", re.MULTILINE)
    prefix = "[학습교재]" if source == "textbook" else "[도움말]"

    chunks = []
    sections: list[tuple[str, str]] = []

    # Split by headings
    last_end = 0
    for m in heading_re.finditer(content):
        if last_end < m.start():
            body = content[last_end:m.start()].strip()
            if body and sections:
                sections[-1] = (sections[-1][0], sections[-1][1] + "\n" + body)
            elif body:
                sections.append(("", body))
        sections.append((m.group(2).strip(), ""))
        last_end = m.end()

    # Remaining text after last heading
    if last_end < len(content):
        tail = content[last_end:].strip()
        if tail and sections:
            sections[-1] = (sections[-1][0], sections[-1][1] + "\n" + tail)
        elif tail:
            sections.append(("", tail))

    if not sections:
        sections = [("", content.strip())]

    # Build chunks
    for i, (heading, body) in enumerate(sections):
        heading_line = f"{prefix} {heading}" if heading else ""
        full_text = f"{heading_line}\n{body}".strip() if heading_line else body.strip()

        if len(full_text) < 30:
            continue

        # Strip markdown decorations
        full_text = strip_markdown_decorations(full_text)

        # Split long chunks
        parts = split_long_text(full_text, MAX_CHUNK_LENGTH)
        for j, part in enumerate(parts):
            part = part.strip()
            if len(part) < 50:
                continue

            # Prepend heading to continuation chunks
            chunk_text = part
            if j > 0 and heading_line and not part.startswith(prefix):
                chunk_text = f"{heading_line} (계속)\n{part}"

            # Enforce max length
            if len(chunk_text) > MAX_CHUNK_LENGTH + OVERLAP_LENGTH:
                chunk_text = chunk_text[:MAX_CHUNK_LENGTH + OVERLAP_LENGTH]

            chunks.append({
                "id": f"{source}_{file_name}_s{i}_{j}",
                "text": chunk_text,
                "source": source,
                "category": heading or file_name,
                "embedding": None,
            })

    return chunks


def embed_text(text: str) -> list[float]:
    """Get embedding from Ollama."""
    resp = requests.post(
        f"{OLLAMA_URL}/api/embed",
        json={"model": EMBED_MODEL, "input": text, "keep_alive": "30m"},
        timeout=120,
    )
    resp.raise_for_status()
    data = resp.json()
    return data["embeddings"][0]


def main():
    # Check Ollama
    try:
        r = requests.get(f"{OLLAMA_URL}/api/tags", timeout=5)
        r.raise_for_status()
    except Exception as e:
        print(f"Ollama not running: {e}")
        sys.exit(1)

    # Collect all .md files
    if not os.path.isdir(DATA_DIR):
        print(f"Data directory not found: {DATA_DIR}")
        sys.exit(1)

    md_files = sorted(f for f in os.listdir(DATA_DIR) if f.endswith(".md"))
    print(f"Found {len(md_files)} markdown files")

    # Parse into chunks
    all_chunks = []
    for md_file in md_files:
        path = os.path.join(DATA_DIR, md_file)
        source = "textbook" if md_file.startswith("textbook_") else "help"
        chunks = split_markdown_into_chunks(path, source)
        all_chunks.extend(chunks)
        print(f"  {md_file}: {len(chunks)} chunks")

    print(f"\nTotal chunks: {len(all_chunks)}")
    print(f"Generating embeddings with {EMBED_MODEL}...")

    # Warmup
    print("  Warming up model...")
    embed_text("warmup")

    # Embed all chunks
    failed = 0
    for i, chunk in enumerate(all_chunks):
        try:
            chunk["embedding"] = embed_text(chunk["text"])
        except Exception as e:
            print(f"  FAILED chunk {chunk['id']}: {e}")
            failed += 1
            chunk["embedding"] = None

        if (i + 1) % 10 == 0 or i == len(all_chunks) - 1:
            print(f"  {i + 1}/{len(all_chunks)} done")

    # Save
    with open(OUTPUT_PATH, "w", encoding="utf-8") as f:
        json.dump(all_chunks, f, ensure_ascii=False)

    embedded = sum(1 for c in all_chunks if c["embedding"] is not None)
    size_mb = os.path.getsize(OUTPUT_PATH) / (1024 * 1024)
    print(f"\nSaved: {OUTPUT_PATH}")
    print(f"  Chunks: {len(all_chunks)} ({embedded} with embeddings, {failed} failed)")
    print(f"  File size: {size_mb:.1f} MB")


if __name__ == "__main__":
    main()
