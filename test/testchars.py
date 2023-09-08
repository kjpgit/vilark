#!/usr/bin/python3
import os

dirname="/tmp/testchars"
os.makedirs(dirname, exist_ok=True)

def make_file(n):
    path = dirname + "/" + n
    open(path, "w")

ascii_tree = [chr(i) for i in range(32,127)]
ascii_tree.remove('/')
ascii_tree = "".join(ascii_tree)

make_file("Combining    |\u0423\u0306|...")
make_file("UTF8 Grin    |😬|...")
make_file("UTF8 Smile   |😊|...")
make_file("UTF8 Normal  |♠♥♦♣|")
make_file("Super Long File " + ("X")*100 + " Ending Now.txt")

# Chars that need to be escaped by vim fnameescape()
make_file("ASCII XMAS TREE "+ ascii_tree + ".txt")
make_file("<CR> File.txt")
make_file("CR File 2<CR>.txt")
make_file("+Plus File.txt")
make_file(":Colon File.txt")
make_file(";Semi Colon File.txt")

