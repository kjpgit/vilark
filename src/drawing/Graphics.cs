// Copyright (C) 2023 Karl Pickett / ViLark Project
namespace vilark;

using UTF8Str = ReadOnlySpan<byte>;

class BoxChars
{
    /* Drawing Characters */
    static public UTF8Str BOX_BLANK        => " "u8;

    static public UTF8Str BOX_VERT         => "│"u8;
    static public UTF8Str BOX_HORIZ        => "─"u8;

    static public UTF8Str BOX_TOP_LEFT     => "┌"u8;
    static public UTF8Str BOX_TOP_RIGHT    => "┐"u8;

    static public UTF8Str BOX_BOTTOM_LEFT  => "└"u8;
    static public UTF8Str BOX_BOTTOM_RIGHT => "┘"u8;

    static public UTF8Str BOX_TEE_LEFT     => "┤"u8;
    static public UTF8Str BOX_TEE_RIGHT    => "├"u8;

    static public UTF8Str SCROLLBAR_INDICATOR     => "▓"u8;
    static public UTF8Str SCROLLBAR_BACKGROUND    => "░"u8;
}
