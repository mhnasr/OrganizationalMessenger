// تبدیل روش لاگین
document.querySelectorAll('.login-tab-btn').forEach(btn => {
    btn.addEventListener('click', function () {
        // بروزرسانی دکمه‌ها
        document.querySelectorAll('.login-tab-btn').forEach(b => b.classList.remove('active'));
        this.classList.add('active');

        // نمایش فرم مربوطه
        const method = this.dataset.method;
        document.querySelectorAll('.login-method').forEach(m => m.classList.remove('active'));
        document.querySelector(`.login-method[data-method="${method}"]`).classList.add('active');

        // تنظیم action فرم
        const form = document.getElementById('loginForm');
        form.action = `/Account/Login?method=${method}`;
    });
});

// ارسال OTP
function sendOtp() {
    const phoneNumber = document.querySelector('[name="phoneNumber"]').value;
    alert('sf');
    if (!phoneNumber || !/09\d{9}/.test(phoneNumber)) {
        alert('لطفا شماره موبایل معتبر وارد کنید');
        return;
    }

    fetch('/Account/SendOtp', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
        },
        body: JSON.stringify({ phoneNumber: phoneNumber })
    })
        .then(r => r.json())
        .then(data => {
            if (data.success) {
                document.getElementById('otpSection').style.display = 'block';
                document.getElementById('submitOtp').style.display = 'block';
                startOtpTimer();
            } else {
                alert(data.message);
            }
        });
}

// تایمر OTP
function startOtpTimer() {
    let seconds = 60;
    const timer = setInterval(() => {
        document.getElementById('otpTimer').textContent = seconds--;
        if (seconds < 0) {
            clearInterval(timer);
            document.getElementById('otpSection').style.display = 'none';
        }
    }, 1000);
}