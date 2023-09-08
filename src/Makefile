build:
	dotnet msbuild

run:
	../artifacts/bin/program/debug/vilark || bash -c 'sleep 2; reset'

runt:
	#strace -e abbrev=none -f -o tt -e ioctl ../artifacts/bin/program/debug/vilark
	strace -f -o tt ../artifacts/bin/program/debug/vilark

stats:
	sloccount .

publish:
	dotnet publish
	cp ../artifacts/publish/program/release/vilark ../artifacts

record:
	terminalizer record /tmp/d2 -k

gif:
	# BUG: Put this at the front of recorded demo
	#  - delay: 0
	#    content: "\r\n"
	rm -f ~/incoming/ttygif/d2.gif
	DISPLAY=:0 terminalizer render /tmp/d2 -o /tmp/t.gif
	cp /tmp/t.gif ~/incoming/ttygif/d2.gif
