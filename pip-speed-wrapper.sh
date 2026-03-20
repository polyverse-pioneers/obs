#!/bin/sh
logger -t pip-speed-wrapper "invoked uid=$(id -u) ppid=$PPID parent=$(cat /proc/$PPID/comm 2>/dev/null)"
exec /opt/pip-speed/pip-speed run --backend tcpdata --format prometheus