class ExamTracker {
    constructor(submissionId, flushIntervalMs = 8000) {
        this.submissionId = submissionId;
        this.events = [];
        this.flushIntervalMs = flushIntervalMs;
        this._pendingTabAction = null;
        this._forceSubmitted = false;
        this._blurTime = 0;

        // ── Block copy / cut / context-menu ──────────────────
        document.addEventListener('copy', (e) => {
            e.preventDefault();
            this._log('COPY_ATTEMPT');
            this._showToast('❌ Không thể sao chép nội dung bài thi');
        });
        document.addEventListener('cut', (e) => {
            e.preventDefault();
            this._log('CUT_ATTEMPT');
        });
        document.addEventListener('contextmenu', (e) => {
            e.preventDefault();
        });
        document.addEventListener('paste', () => this._log('PASTE'));

        // ── Block keyboard shortcuts (Ctrl/Meta + C/A/P/S) + PrintScreen ──
        document.addEventListener('keydown', (e) => {
            const key = e.key.toLowerCase();
            if (e.key === 'PrintScreen') {
                e.preventDefault();
                this._log('SCREENSHOT_KEY');
                this._reportScreenshot('PrintScreen');
                return;
            }
            if ((e.ctrlKey || e.metaKey) && ['c', 'a', 'p', 's', 'u'].includes(key)) {
                e.preventDefault();
                if (key === 'c') {
                    this._log('COPY_ATTEMPT');
                    this._showToast('❌ Không thể sao chép nội dung bài thi');
                }
            }
        });

        // ── Tab switch detection ──────────────────────────────
        document.addEventListener('visibilitychange', () => {
            if (document.hidden) {
                this._log('TAB_LEAVE');
                this._handleTabLeave();
            } else {
                this._log('TAB_RETURN');
                this._handleTabReturn();
            }
        });

        // ── Screenshot heuristic: very brief blur (<500 ms) ──
        window.addEventListener('blur', () => {
            this._blurTime = Date.now();
            this._log('WINDOW_BLUR');
        });
        window.addEventListener('focus', () => {
            const elapsed = Date.now() - this._blurTime;
            this._log('WINDOW_FOCUS');
            // Brief unfocus (<500 ms, >50 ms) → likely screenshot tool (macOS Cmd+Shift+4, etc.)
            if (elapsed > 50 && elapsed < 500 && this._blurTime > 0) {
                this._log('SCREENSHOT_ATTEMPT');
                this._reportScreenshot('brief-blur');
            }
            this._blurTime = 0;
        });

        document.addEventListener('fullscreenchange', () => {
            if (!document.fullscreenElement) this._log('FULLSCREEN_EXIT');
        });

        const form = document.getElementById('examForm');
        if (form) {
            form.addEventListener('submit', () => {
                this._log('EXAM_SUBMIT');
                this.flush();
                clearInterval(this._interval);
            });
        }

        this._interval = setInterval(() => this._periodicFlush(), this.flushIntervalMs);
    }

    _log(type) {
        this.events.push({ type: type, timestamp: Date.now() });
    }

    async _handleTabLeave() {
        try {
            const resp = await fetch('/api/ExamBehavior/tab-switch', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ submissionId: this.submissionId })
            });
            if (!resp.ok) return;
            const data = await resp.json();
            this._pendingTabAction = data;

            if (data.action === 'force_submit') {
                this._doForceSubmit();
            }
        } catch (_) { /* network error — ignore */ }
    }

    _handleTabReturn() {
        if (!this._pendingTabAction) return;
        const { action } = this._pendingTabAction;
        this._pendingTabAction = null;

        if (action === 'force_submit') {
            this._showWarningModal(
                '🚫 Bài thi bị thu',
                'Bạn đã rời khỏi trang thi 3 lần. Bài làm đang được nộp tự động với điểm 0 vì gian lận.',
                'danger'
            );
            setTimeout(() => this._doForceSubmit(), 2000);
            return;
        }

        if (action === 'warn') {
            this._showWarningModal(
                '⚠️ Cảnh báo lần 1 / 3',
                'Bạn vừa rời khỏi trang thi. Còn 2 lần nữa bài thi sẽ tự động bị nộp với điểm 0.',
                'warning'
            );
        } else if (action === 'notify_tutor') {
            this._showWarningModal(
                '⚠️ Cảnh báo lần 2 / 3 — Đã thông báo giáo viên',
                'Giáo viên đã được thông báo về hành vi của bạn. Lần rời trang tiếp theo bài thi sẽ TỰ ĐỘNG NỘP với điểm 0.',
                'danger'
            );
        }
    }

    _doForceSubmit() {
        if (this._forceSubmitted) return;
        this._forceSubmitted = true;
        const isFraudField = document.getElementById('isFraud');
        if (isFraudField) isFraudField.value = 'true';
        const form = document.getElementById('examForm');
        if (form) form.submit();
    }

    async _reportScreenshot(method) {
        this._showToast('⚠️ Hành vi chụp màn hình đã được ghi nhận và gửi cho giáo viên');
        try {
            await fetch('/api/ExamBehavior/screenshot', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ submissionId: this.submissionId, method })
            });
        } catch (_) { /* ignore network error */ }
    }

    _showWarningModal(title, message, type) {
        const modal = document.getElementById('tabWarningModal');
        if (!modal) return;
        document.getElementById('tabWarningTitle').textContent = title;
        document.getElementById('tabWarningMessage').textContent = message;
        modal.className = `tab-warning-modal tab-warning-${type}`;
        modal.style.display = 'flex';
    }

    _showToast(message) {
        const container = document.getElementById('examToastContainer');
        if (!container) return;
        const toast = document.createElement('div');
        toast.className = 'exam-toast';
        toast.textContent = message;
        container.appendChild(toast);
        // Trigger animation
        requestAnimationFrame(() => toast.classList.add('exam-toast-show'));
        setTimeout(() => {
            toast.classList.remove('exam-toast-show');
            setTimeout(() => toast.remove(), 400);
        }, 3000);
    }

    _periodicFlush() {
        if (this.events.length === 0) return;
        const payload = {
            submissionId: this.submissionId,
            events: this.events.splice(0)
        };
        fetch('/api/ExamBehavior/log', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(payload),
            keepalive: true
        }).catch(() => {
            this.events.unshift(...payload.events);
        });
    }

    flush() {
        if (this.events.length === 0) return;
        const payload = {
            submissionId: this.submissionId,
            events: this.events.splice(0)
        };
        navigator.sendBeacon(
            '/api/ExamBehavior/log',
            new Blob([JSON.stringify(payload)], { type: 'application/json' })
        );
    }
}

window.ExamTracker = ExamTracker;
