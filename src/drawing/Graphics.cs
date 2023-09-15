// Copyright (C) 2023 Karl Pickett / Vilark Project
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

    static public UTF8Str LIGHT_SHADE      => "░"u8;
    static public UTF8Str MEDIUM_SHADE     => "▒"u8;
    static public UTF8Str DARK_SHADE       => "▓"u8;

    static public UTF8Str SCROLLBAR_INDICATOR     => "▓"u8;
    static public UTF8Str SCROLLBAR_BACKGROUND    => "░"u8;

    static public UTF8Str CURVE_TOP_LEFT          => "╭"u8;
    static public UTF8Str CURVE_TOP_RIGHT         => "╮"u8;

    static public UTF8Str CURVE_BOTTOM_LEFT       => "╰"u8;
    static public UTF8Str CURVE_BOTTOM_RIGHT      => "╯"u8;

    static public UTF8Str LEFT_ARROW_SOLID        => "◀"u8;
    static public UTF8Str RIGHT_ARROW_SOLID       => "▶"u8;

    static public UTF8Str RIGHT_BLOCK_18          => "▕"u8;
    static public UTF8Str RIGHT_BLOCK_12          => "▐"u8;
    static public UTF8Str LEFT_BLOCK_18           => "▏"u8;
    static public UTF8Str LEFT_BLOCK_12           => "▌"u8;

}
