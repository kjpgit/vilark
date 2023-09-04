#!/usr/bin/python
import os

dirname="/tmp/testchars"
os.makedirs(dirname, exist_ok=True)

def make_file(n):
    path = dirname + "/" + n
    open(path, "w")

make_file("Combining    |\u0423\u0306|...")
make_file("UTF8 Grin    |😬|...")
make_file("UTF8 Smile   |😊|...")
make_file("UTF8 Normal  |♠♥♦♣|")
make_file("Super Long File " + ("X")*100 + " Ending Now.txt")

