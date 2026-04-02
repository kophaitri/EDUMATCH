class ExamTracker {
    constructor(submissionId, flushIntervalMs = 10000) {
        this.submissionId = submissionId;
        this.events = [];
        this.flushIntervalMs = flushIntervalMs;

        document.addEventListener('copy', () => this._log('COPY'));
        document.addEventListener('paste', () => this._log('PASTE'));
        document.addEventListener('visibilitychange', () => {
            this._log(document.hidden ? 'TAB_LEAVE' : 'TAB_RETURN');
        });
        window.addEventListener('blur', () => this._log('WINDOW_BLUR'));
        window.addEventListener('focus', () => this._log('WINDOW_FOCUS'));
        window.addEventListener('beforeunload', () => this.flush());

        this._interval = setInterval(() => this.flush(), this.flushIntervalMs);
    }

    _log(type) {
        this.events.push({ type: type, timestamp: Date.now() });
    }

    flush() {
        if (this.events.length === 0) return;

        const payload = {
            submissionId: this.submissionId,
            events: this.events.splice(0)
        };

        navigator.sendBeacon('/api/ExamBehavior/log',
            new Blob([JSON.stringify(payload)], { type: 'application/json' })
        );
    }
}

window.ExamTracker = ExamTracker;
