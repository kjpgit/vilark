# Mac OS Notes

## Captured Data

    https://brew.sh/
    brew install vim
    ~/.vim[/plugin] and ~/.vimrc works as normal.

    ip-172-31-0-104:~ ec2-user$ echo $HOME
    /Users/ec2-user

    ip-172-31-0-104:~ ec2-user$ echo $PATH
    /opt/homebrew/bin:/opt/homebrew/sbin:/Users/ec2-user/.local/bin:/usr/local/bin:/System/Cryptexes/App/usr/bin:/usr/bin:/bin:/usr/sbin:/sbin:/var/run/com.apple.security.cryptexd/codex.system/bootstrap/usr/local/bin:/var/run/com.apple.security.cryptexd/codex.system/bootstrap/usr/bin:/var/run/com.apple.security.cryptexd/codex.system/bootstrap/usr/appleinternal/bin

    ip-172-31-0-104:~ ec2-user$ echo $OSTYPE $HOSTTYPE
    darwin22 arm64

    ip-172-31-0-104:~ ec2-user$ which bash
    /bin/bash

    ip-172-31-0-104:~ ec2-user$ which sh
    /bin/sh

    bash --version
    GNU bash, version 3.2.57(1)-release (arm64-apple-darwin22)

    ip-172-31-0-104:~ ec2-user$ sh --version
    GNU bash, version 3.2.57(1)-release (arm64-apple-darwin22)

    ip-172-31-0-104:~ ec2-user$ curl --version
    curl 8.1.2 (x86_64-apple-darwin22.0) libcurl/8.1.2 (SecureTransport) LibreSSL/3.3.6 zlib/1.2.11 nghttp2/1.51.0
    Release-Date: 2023-05-30
     Protocols: dict file ftp ftps gopher gophers http https imap imaps ldap
    ldaps mqtt pop3 pop3s rtsp smb smbs smtp smtps telnet tftp Features: alt-svc
    AsynchDNS GSS-API HSTS HTTP2 HTTPS-proxy IPv6 Kerberos
    Largefile libz MultiSSL NTLM NTLM_WB SPNEGO SSL threadsafe UnixSockets

    ip-172-31-0-104:~ ec2-user$ which install
    /usr/bin/install
    The install utility appeared in 4.2BSD.

    ip-172-31-0-104:~ ec2-user$ which tar
    /usr/bin/tar
    bsdtar 3.5.3 - libarchive 3.5.3 zlib/1.2.11 liblzma/5.0.5 bz2lib/1.0.8

    $ which stty
    /bin/stty
    A stty command appeared in Version 2 AT&T UNIX.

    stty -g
    gfmt1:cflag=4b00:iflag=2106:lflag=cf:oflag=3:discard=f:dsusp=19:eof=4:eol=0:eol2=0:erase=7f:intr=3:kill=15:lnext=16:min=1:quit=1c:reprint=12:start=11:status=14:stop=13:susp=1a:time=0:werase=17:ispeed=38400:ospeed=38400

    stty `stty -g`  # works

    tput init does not work to enable the echo, neither does tput reset.  stty
    must be used

    libSystem path: not on the file system since mac os 11.  There is an
    invisible "dynamink library cache".  However, dlopen() on it works as normal.
    It is designed to be a transparent optimization.

## Testing

* use stty sane
* turn off 'selected file is X message, it's long'
* terminal.app does NOT work with colors. Iterm2 is fine.
* add ~/.local/bin to PATH
* use brew vim, regular doesn't have python (-python3)

✅ path check works
✅ exec works
✅ sigstop works
