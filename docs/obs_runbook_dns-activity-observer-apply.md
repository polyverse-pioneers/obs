# Runbook: Apply DNS Activity Observer To Planck

## Objective

Apply the locally tracked DNS activity observer changes from WSL on `quantum` to
the live `planck` host, then validate that Telegraf, Unbound, and Prometheus
all see the new bounded DNS activity metrics.

This observer is intended to surface the noisy query names and upstream DNS
destinations that matter operationally, such as ad or tracker domains you may
want to filter later, without turning Prometheus into a raw DNS event store.

## Assumptions

- You are running these commands locally on WSL on `quantum`.
- `planck` is reachable as `planck-primary`.
- The repo copies on WSL contain the desired source of truth.
- Prometheus and Grafana are already running on `planck`.

## Files Being Applied

From the `obs` repo:

- `scripts/dns_activity_observer.py` -> `/opt/obs/dns_activity_observer.py`
- `backups/etc/telegraf/telegraf.d/dns-activity-observer.conf` -> `/etc/telegraf/telegraf.d/dns-activity-observer.conf`
- `backups/etc/systemd/system/telegraf.service.d/override.conf` -> `/etc/systemd/system/telegraf.service.d/override.conf`

From the `dns` repo:

- `backups/planck/etc/unbound/unbound.conf.d/home-lab.conf` -> `/etc/unbound/unbound.conf.d/home-lab.conf`

## 1. Copy The Staged Files To `/tmp` On Planck

From WSL on `quantum`:

```bash
cd /home/spinriko/polyverse-pioneers/obs
scp scripts/dns_activity_observer.py planck-primary:/tmp/dns_activity_observer.py
scp backups/etc/telegraf/telegraf.d/dns-activity-observer.conf planck-primary:/tmp/dns-activity-observer.conf
scp backups/etc/systemd/system/telegraf.service.d/override.conf planck-primary:/tmp/telegraf-override.conf

cd /home/spinriko/personal/dns
scp backups/planck/etc/unbound/unbound.conf.d/home-lab.conf planck-primary:/tmp/home-lab.conf
```

## 2. Install The Files On Planck

From WSL on `quantum`:

```bash
ssh planck-primary <<'EOF'
set -euo pipefail

sudo install -d -m 755 /opt/obs
sudo install -d -m 755 /etc/telegraf/telegraf.d
sudo install -d -m 755 /etc/systemd/system/telegraf.service.d

sudo install -m 755 /tmp/dns_activity_observer.py /opt/obs/dns_activity_observer.py
sudo install -m 644 /tmp/dns-activity-observer.conf /etc/telegraf/telegraf.d/dns-activity-observer.conf
sudo install -m 644 /tmp/telegraf-override.conf /etc/systemd/system/telegraf.service.d/override.conf
sudo install -m 644 /tmp/home-lab.conf /etc/unbound/unbound.conf.d/home-lab.conf
EOF
```

## 3. Pre-Restart Validation On Planck

From WSL on `quantum`:

```bash
ssh planck-primary <<'EOF'
set -euo pipefail

sudo /usr/bin/python3 -m py_compile /opt/obs/dns_activity_observer.py
sudo unbound-checkconf
sudo /usr/bin/python3 /opt/obs/dns_activity_observer.py | sed -n '1,40p'
sudo /usr/bin/telegraf --test --config /etc/telegraf/telegraf.conf --config-directory /etc/telegraf/telegraf.d
EOF
```

Expected outcome:

- Python compile succeeds with no output.
- `unbound-checkconf` exits cleanly.
- The exporter prints Prometheus text including `dns_activity_observer_scrape_success`.
- `telegraf --test` shows the new `dns_activity_observer_*` metrics and no config errors.

## 4. Reload Services On Planck

From WSL on `quantum`:

```bash
ssh planck-primary <<'EOF'
set -euo pipefail

sudo systemctl daemon-reload
sudo systemctl restart unbound
sudo systemctl restart telegraf
sudo systemctl is-active unbound
sudo systemctl is-active telegraf
EOF
```

Expected outcome:

- Both services report `active`.

## 5. Validate Runtime Behavior On Planck

From WSL on `quantum`:

```bash
ssh planck-primary <<'EOF'
set -euo pipefail

sudo journalctl -u unbound -n 30 --no-pager
sudo journalctl -u telegraf -n 60 --no-pager

curl -sG http://127.0.0.1:9090/api/v1/query \
  --data-urlencode 'query=dns_activity_observer_scrape_success' | jq .

curl -sG http://127.0.0.1:9090/api/v1/query \
  --data-urlencode 'query=dns_activity_observer_top_query_count' | jq .

curl -sG http://127.0.0.1:9090/api/v1/query \
  --data-urlencode 'query=dns_activity_observer_top_upstream_count' | jq .
EOF
```

Expected outcome:

- `unbound` journal shows query/reply activity.
- `telegraf` journal does not show permission or exec errors.
- Prometheus returns series for `dns_activity_observer_scrape_success`.
- Prometheus returns top-query and top-upstream series once there is enough recent DNS activity.

## 6. Grafana Follow-Through

After Prometheus confirms the new series, import or provision:

- `grafana-dashboards/dash-F-dns-activity-observer.json`

Then validate that the dashboard populates:

- Observer scrape status
- Top query names by count and share
- Top upstream destinations by count and share

## Failure Checks

If `telegraf` fails after restart:

- Re-read `journalctl -u telegraf -n 100 --no-pager`
- Confirm `/etc/systemd/system/telegraf.service.d/override.conf` includes `SupplementaryGroups=systemd-journal`
- Confirm `daemon-reload` happened before the service restart

If the exporter prints zero events:

- Confirm `unbound` is actually emitting recent query and reply logs to journald
- Generate a few lookups on Planck with `dig +short @127.0.0.1 example.com A`
- Re-run `/usr/bin/python3 /opt/obs/dns_activity_observer.py`

If Prometheus has scrape-success but no top series yet:

- Wait for the next Telegraf interval
- Generate a few DNS requests from a client or from Planck itself
- Re-run the Prometheus queries above
