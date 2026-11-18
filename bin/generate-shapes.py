#!/usr/bin/env python3
"""Write the shapes/ folder from the history of the exercism/fsharp track repo.

A shape is a distinct xUnit v3 project file, ignoring the project name, its Compile items and the
settings that don't affect compiling. This script reads every exercise's .fsproj in every commit since
the track switched to xUnit v3, groups them by shape, and writes one template project file per shape to
<out_dir>/<n>/ExercismShape.fsproj, with net9.0 replaced by net10.0 as the runner does. The Dockerfile
builds each one (bin/build-shapes.sh), so that the fast path can reuse its compiler arguments.

The grouping here is close to FastPath.shapeKey in the runner. If the two ever disagree, the worst case
will be a duplicate template, or a project file with no matching template, which uses MSBuild.

usage: generate-shapes.py <out_dir> [path to the exercism/fsharp repo, default ../fsharp]"""
import os, re, shutil, subprocess, sys, xml.etree.ElementTree as ET

track = sys.argv[2] if len(sys.argv) > 2 else '../fsharp'
out = sys.argv[1]
IGNORED = {'IsPackable', 'RootNamespace'}

def git(*args):
    return subprocess.run(['git', '-C', track, *args], capture_output=True, check=True, text=True).stdout

def key(text):
    root = ET.fromstring(text.lstrip('﻿'))
    props, items = {}, []
    for group in root:
        for e in group:
            if group.tag == 'PropertyGroup':
                if e.tag not in IGNORED: props[e.tag] = (e.text or '').strip()
            elif e.tag != 'Compile':
                items.append((e.tag, tuple(sorted(e.attrib.items())) + tuple(sorted((m.tag, (m.text or '').strip()) for m in e))))
    return (tuple(sorted(props.items())), tuple(sorted(items)))

def template(text):
    # Keep the project file as it is, with the Compile items replaced by the template's two files.
    text = text.lstrip('﻿').replace('net9.0', 'net10.0')
    text = re.sub(r'(\s*<Compile Include="[^"]*" />)+',
                  '\n    <Compile Include="ExercismShape.fs" />\n    <Compile Include="ExercismShapeTests.fs" />', text, count=1)
    return re.sub(r'\s*<RootNamespace>[^<]*</RootNamespace>', '', text)

commits = git('log', '--format=%H', '-S', 'xunit.v3', '--', 'exercises').split()
since = commits[-1] if commits else 'HEAD'
shapes = {}
for commit in git('log', '--format=%H', f'{since}~1..HEAD', '--', 'exercises').split() + ['HEAD']:
    for path in git('ls-tree', '-r', '--name-only', commit, 'exercises').split('\n'):
        if not path.endswith('.fsproj'): continue
        text = git('show', f'{commit}:{path}').replace('net9.0', 'net10.0')
        if 'xunit.v3' not in text: continue
        shapes.setdefault(key(text), (text, set()))[1].add(path.split('/')[2])

shutil.rmtree(out, ignore_errors=True)
for i, (k, (text, exercises)) in enumerate(sorted(shapes.items(), key=lambda kv: (-len(kv[1][1]), str(kv[0])))):
    d = os.path.join(out, f'{i:02d}')
    os.makedirs(d)
    open(os.path.join(d, 'ExercismShape.fsproj'), 'w').write(template(text))
    print(i, len(exercises), sorted(exercises)[:3])
