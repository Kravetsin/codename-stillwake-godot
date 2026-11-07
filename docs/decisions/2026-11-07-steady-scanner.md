# A steady scanner readout

- Status: accepted
- Date: 2026-11-07
- Amends [the-scanner](2026-11-07-the-scanner.md)

## Context

In play the readout shook enough to make the user feel sick. Its pixels were the world's, at the camera's fractional zoom, so
every digit grew and shrank by a screen pixel as the camera eased; its scanlines ran along the screen's rows and shimmered as
the box moved over them; its brightness flickered at random 30 times a second and its grain jumped 24 times a second; and the
box narrowed by a digit as the value fell from 100 to 99.

## Decision

- **Whole screen pixels:** a readout pixel takes the zoom rounded, at least one (`Phosphor.Scale`), drawn from the point it
  reads, so a digit keeps its shape and the box only slides.
- **Its own scanlines,** every other screen row counted from the box's top, moving with it; the shader's are off.
- **No flicker and no grain:** it fades in and out evenly. The slow rolling line and the flare as the value falls stay.
- **A fixed width:** the value left has room for every digit of the maximum, right-aligned (`Phosphor.LeftWidth`).

## Deliberately not handled

- Below a zoom of 1.5 a readout pixel is one screen pixel and the box has no scanlines: no player zooms out so far.
