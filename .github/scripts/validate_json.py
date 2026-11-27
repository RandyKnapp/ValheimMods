#!/usr/bin/env python3
"""Checks the syntax of every tracked .json and .jsonc file (or the files given as arguments).

EpicLoot reads its JSON with Newtonsoft, which skips // and /* */ comments, and the config patch
examples rely on that. jq does not accept comments, so this blanks them out with spaces (keeping
every line and column number where it was) and parses what is left strictly: trailing commas,
NaN, Infinity and other non-JSON are still errors.

Errors are printed as GitHub annotations, so they show on the file and line in a pull request.
"""
import json
import os
import subprocess
import sys


class CommentError(ValueError):
    def __init__(self, msg, pos):
        super().__init__(msg)
        self.msg = msg
        self.pos = pos


def strip_comments(text):
    """Returns text with every comment outside a string replaced by spaces; newlines are kept."""
    out = list(text)
    i, n = 0, len(text)
    in_string = False
    while i < n:
        c = text[i]
        if in_string:
            if c == '\\':
                i += 2
                continue
            if c == '"':
                in_string = False
            i += 1
            continue
        if c == '"':
            in_string = True
            i += 1
            continue
        if c == '/' and i + 1 < n and text[i + 1] == '/':
            while i < n and text[i] != '\n':
                out[i] = ' '
                i += 1
            continue
        if c == '/' and i + 1 < n and text[i + 1] == '*':
            end = text.find('*/', i + 2)
            if end < 0:
                raise CommentError('Unterminated /* comment', i)
            for j in range(i, end + 2):
                if text[j] != '\n':
                    out[j] = ' '
            i = end + 2
            continue
        i += 1
    return ''.join(out)


def line_col(text, pos):
    line = text.count('\n', 0, pos) + 1
    col = pos - text.rfind('\n', 0, pos)
    return line, col


def reject_constant(name):
    raise ValueError(f'{name} is not valid JSON')


def escape_data(s):
    return s.replace('%', '%25').replace('\r', '%0D').replace('\n', '%0A')


def escape_property(s):
    return escape_data(s).replace(':', '%3A').replace(',', '%2C')


def report(path, msg, line=None, col=None):
    props = f'file={escape_property(path)}'
    if line is not None:
        props += f',line={line},col={col}'
    print(f'::error {props}::{escape_data(msg)}')


def tracked_files():
    result = subprocess.run(
        ['git', 'ls-files', '-z', '--', '*.json', '*.jsonc'],
        check=True, capture_output=True)
    return [f for f in result.stdout.decode('utf-8').split('\0') if f]


def check(path):
    """Returns True when the file parses."""
    try:
        # utf-8-sig accepts a byte order mark, as Newtonsoft and jq do.
        with open(path, encoding='utf-8-sig') as fh:
            text = fh.read()
    except (OSError, UnicodeDecodeError) as e:
        report(path, f'Could not read: {e}')
        return False

    try:
        json.loads(strip_comments(text), parse_constant=reject_constant)
    except CommentError as e:
        report(path, e.msg, *line_col(text, e.pos))
        return False
    except json.JSONDecodeError as e:
        report(path, e.msg, e.lineno, e.colno)
        return False
    except ValueError as e:
        report(path, str(e))
        return False
    return True


def main():
    # git ls-files also lists tracked files deleted from the working tree; there is nothing to check.
    files = [f for f in (sys.argv[1:] or tracked_files()) if os.path.exists(f)]
    failed = [f for f in files if not check(f)]
    print(f'Checked {len(files)} JSON files: {len(failed)} with errors.')
    return 1 if failed else 0


if __name__ == '__main__':
    sys.exit(main())
