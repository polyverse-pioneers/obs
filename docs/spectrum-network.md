
# Simplified Diagram

## Nodes That Matter

```text
[Spectrum Modem/ONT]
        |
        | WAN
        v
[Heisenberg - MikroTik Router]
  ether2 ---------------------> [Bohr - TP-Link SG2008 Switch]
  ether5 ---------------------> [Planck - Raspberry Pi 5]
                                  (Network monitoring)

From Bohr switch:
- VLAN10: Wi-Fi and IoT side
- VLAN20: TV and game devices side
- VLAN40: Lab/expansion side
- VLAN99: Management side
```

## What to say to Spectrum

"Spectrum handoff goes directly into Heisenberg WAN. Heisenberg is our main router. From Heisenberg, ether2 uplinks to our managed switch (Bohr). Planck is our monitoring host on a direct Heisenberg link."