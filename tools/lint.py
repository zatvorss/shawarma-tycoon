#!/usr/bin/env python3
"""Проверка стиля исходников «Шаурма Магнат».

Правила совпадают с .editorconfig:
  ошибки   — не UTF-8, переводы строк CRLF, табуляция в отступах,
             пробелы в конце строки, нет перевода строки в конце файла,
             C#-файл без namespace ValourGames.Shawarma;
  замечания — строка C# длиннее 200 символов (max_line_length).

Запуск:  python3 tools/lint.py            — весь Assets/
         python3 tools/lint.py файл1 ...  — только указанные файлы
Код возврата 1, если есть ошибки (pre-commit тогда не даёт закоммитить).
"""
import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
EXTS = (".cs", ".jslib", ".html")
MAX_LEN = 200
NAMESPACE = "namespace ValourGames.Shawarma"


def collect(paths):
    if paths:
        return [p for p in paths if p.endswith(EXTS) and os.path.isfile(p)]
    found = []
    for base, _, files in os.walk(os.path.join(ROOT, "Assets")):
        found += [os.path.join(base, f) for f in files if f.endswith(EXTS)]
    return sorted(found)


def check(path):
    errors, warnings = [], []
    raw = open(path, "rb").read()
    try:
        text = raw.decode("utf-8")
    except UnicodeDecodeError:
        return ["файл не в UTF-8"], []
    if b"\r\n" in raw:
        errors.append("переводы строк CRLF (нужен LF)")
    if raw and not raw.endswith(b"\n"):
        errors.append("нет перевода строки в конце файла")
    for n, line in enumerate(text.split("\n"), 1):
        indent = line[: len(line) - len(line.lstrip())]
        if "\t" in indent:
            errors.append(f"{n}: табуляция в отступе")
        body = line.rstrip("\r")
        if body != body.rstrip(" \t"):
            errors.append(f"{n}: пробелы в конце строки")
        if path.endswith(".cs") and len(line) > MAX_LEN:
            warnings.append(f"{n}: строка {len(line)} символов (> {MAX_LEN})")
    if path.endswith(".cs") and NAMESPACE not in text:
        errors.append(f"нет «{NAMESPACE}»")
    return errors, warnings


def main():
    files = collect(sys.argv[1:])
    total_err = total_warn = 0
    for f in files:
        errors, warnings = check(f)
        rel = os.path.relpath(f, ROOT)
        for e in errors:
            print(f"ОШИБКА  {rel}: {e}")
        total_err += len(errors)
        total_warn += len(warnings)
    print(f"Проверено файлов: {len(files)}; ошибок: {total_err}; длинных строк: {total_warn}")
    return 1 if total_err else 0


if __name__ == "__main__":
    sys.exit(main())
