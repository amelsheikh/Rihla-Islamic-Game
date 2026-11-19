"""Packages Builds/Windows as a prototype the owner can test: start files, a how-to-play note, and a zip.
Usage, after a Windows build: python tools/prototype/package.py 2026-11-19  (writes Builds/Rihla-prototype-<date>.zip)."""
import io, os, shutil, zipfile, sys
root = os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', '..')).replace(os.sep, '/') + '/'
build = root + 'Builds/Windows/'
here = os.path.dirname(os.path.abspath(__file__))  # tools/prototype: this script and HOW TO PLAY.txt
date = sys.argv[1]

starts = {
    '1 - Start the story.bat': '@echo off\r\ncd /d "%~dp0"\r\nstart "" "Rihla.exe" -newgame\r\n',
    '2 - Walk in Al-Quds.bat': '@echo off\r\ncd /d "%~dp0"\r\nstart "" "Rihla.exe" -walk -newgame\r\n',
    '3 - Drive from Al-Quds.bat': '@echo off\r\ncd /d "%~dp0"\r\nstart "" "Rihla.exe" -drive -newgame\r\n',
}
for old in ('1 - Walk in Al-Quds.bat', '2 - Drive from Al-Quds.bat'):  # the names in the first prototype
    if os.path.exists(build + old): os.remove(build + old)
for name, text in starts.items():
    io.open(build + name, 'w', newline='').write(text)
shutil.copy(os.path.join(here, 'HOW TO PLAY.txt'), build + 'HOW TO PLAY.txt')

out = root + f'Builds/Rihla-prototype-{date}.zip'
skip = 'Rihla_BackUpThisFolder_ButDontShipItWithYourGame'
with zipfile.ZipFile(out, 'w', zipfile.ZIP_DEFLATED) as z:
    for d, dirs, files in os.walk(build):
        dirs[:] = [x for x in dirs if x != skip]
        for f in files:
            full = os.path.join(d, f)
            z.write(full, os.path.join('Rihla prototype', os.path.relpath(full, build)))
print(out, round(os.path.getsize(out) / 1e6, 1), 'MB')
