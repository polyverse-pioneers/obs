# Netspeed CLI Help

## Overview

Netspeed is a deterministic network speed test CLI for download, upload, and latency metrics.

Current command set:
- run

## Usage

```text
netspeed run [options]
```

## Run Command Options

- --backend <tcpdata|custom>
: Select backend provider.
: Default: tcpdata.

- --download-size <bytes>
: Download payload size in bytes.
: Default: 10485760.

- --upload-size <bytes>
: Upload payload size in bytes.
: Default: 10485760.
: Use 0 to skip upload when backend behavior permits.

- --latency-samples <int>
: Number of latency probes.
: Default: 10.

- --concurrency <int>
: Reserved for future multi-stream support.
: Default: 1.

- --timeout <seconds>
: Per-request timeout in seconds.
: Default: 30.

- --download-url <url>
: Required when backend is custom.

- --upload-url <url>
: Optional when backend is custom.
: If omitted, upload is skipped and upload metrics remain zero-valued.

- --format <json|text|prometheus>
: Output formatter selection.
: Default: json.

- --label <key=value>
: Repeatable metadata label.
: Example: --label host=planck --label region=home.

## Examples

```bash
netspeed run
```

```bash
netspeed run --backend tcpdata --format json
```

```bash
netspeed run --backend custom --download-url https://example.test/download --format text
```

```bash
netspeed run --backend custom --download-url https://example.test/download --upload-url https://example.test/upload --upload-size 5242880 --format prometheus
```

```bash
netspeed run --label host=planck --label region=home
```

## Exit Codes

- 0: success
- 1: CLI or argument validation error
- 2: network or HTTP execution error
- 3: internal error

## Validation Rules

- backend must be tcpdata or custom.
- format must be json, text, or prometheus.
- download-size must be greater than 0.
- upload-size must be 0 or greater.
- latency-samples must be greater than 0.
- concurrency must be greater than 0.
- timeout must be greater than 0.
- download-url must be present and valid when backend is custom.
- upload-url, if provided, must be a valid absolute URL.
- each label must match key=value.
