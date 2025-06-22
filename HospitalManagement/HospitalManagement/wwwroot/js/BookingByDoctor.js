$(document).ready(function () {
    // Ẩn các khu vực ban đầu
    $('#time-slots-container').hide();
    $('.service-title, .service-dropdown').hide();

    // Load time slots từ server
    function loadTimeSlots() {
        const doctorId = $('#doctorDropdown').val();
        const date = $('#appointmentDate').val();

        if (!doctorId || !date) {
            $('#time-slots-container').hide().empty();
            return;
        }

        $('#time-slots-container')
            .html('<div class="loading-slots"><div class="loading-spinner"></div>Đang tải...</div>')
            .css('display', 'grid')
            .show();

        $.ajax({
            url: '/Appointment/GetSlotsByDoctor',
            method: 'GET',
            data: { doctorId: doctorId, date: date },
            success: function (slots) {
                $('#time-slots-container').empty();
                if (!slots || slots.length === 0) {
                    $('#time-slots-container').html(
                        '<div class="no-slots"><i class="fas fa-calendar-times"></i><div>Không có khung giờ nào khả dụng</div></div>'
                    );
                    return;
                }
                let html = '';
                slots.forEach(slot => {
                    const cls = slot.isBooked ? 'time-slot booked' : 'time-slot available';
                    const status = slot.isBooked ? 'Đã đặt' : 'Có thể đặt';
                    const dot = slot.isBooked ? 'booked' : 'available';
                    html += `
<div class="${cls}" data-slot-id="${slot.slotId}">
  <div class="fw-semibold">${slot.slotTime}</div>
  <div class="slot-status">
    <span class="status-dot ${dot}"></span>
    <span>${status}</span>
  </div>
</div>`;
                });
                $('#time-slots-container').html(html);
            },
            error: function () {
                $('#time-slots-container')
                    .css('display', 'grid')
                    .html('<div class="no-slots">Không thể tải dữ liệu khung giờ.</div>');
            }
        });
    }

    // Cập nhật trạng thái nút Đặt Hẹn
    function updateSubmitButton() {
        let valid = $('#doctorDropdown').val()
            && $('#appointmentDate').val()
            && $('#SelectedSlotId').val();
        if ($('.service-dropdown').is(':visible')) {
            valid = valid && $('#serviceDropdown').val();
        }
        $('.submit-btn').prop('disabled', !valid);
    }
    // Khi chọn chuyên khoa -> load danh sách bác sĩ
    $('#departmentDropdown').on('change', function () {
        var deptId = $(this).val();
        const $ddl = $('#doctorDropdown');
        if (!deptId) {
            $('.doctor-search-title, .doctor-search, .doctor-select-title, .doctor-dropdown').hide();
            $ddl.empty().append('<option value="">-- Chọn bác sĩ --</option>');
            $('#time-slots-container').hide().empty();
            $('.service-title,.service-dropdown').hide();
            return;
        }
        $.getJSON('/Appointment/GetDoctorsByDepartment', { departmentId: deptId })
            .done(function (doctors) {
                $ddl.empty().append('<option value="">-- Chọn bác sĩ --</option>');
                doctors.forEach(d => $ddl.append(`<option value="${d.id}">${d.name}</option>`));
                $('.doctor-search-title, .doctor-search, .doctor-select-title, .doctor-dropdown').show();
            })
            .fail(function () {
                alert('Không tải được danh sách bác sĩ');
            });
    });

    // Lọc tên bác sĩ
    $('#doctorSearch').on('input', function () {
        var term = $(this).val().toLowerCase();
        $('#doctorDropdown option').each(function () {
            if (!$(this).val()) return;
            var txt = $(this).text().toLowerCase();
            $(this).prop('hidden', term && !txt.includes(term));
        });
    });
    // Khi chọn bác sĩ
    $('#doctorDropdown').on('change', function () {
        loadTimeSlots();
        $('.service-title, .service-dropdown').hide();
        updateSubmitButton();
    });

    // Khi chọn ngày
    $('#appointmentDate').on('change', function () {
        loadTimeSlots();
        $('.service-title, .service-dropdown').hide();
        updateSubmitButton();
    });

    // Khi chọn slot
    $(document).on('click', '.time-slot.available', function () {
        $('.time-slot.available').removeClass('selected');
        $(this).addClass('selected');
        $('#SelectedSlotId').val($(this).data('slot-id'));

        // Hiển thị phần chọn dịch vụ
        $('.service-title, .service-dropdown').show();
        updateSubmitButton();
    });

    // Khi chọn dịch vụ
    $(document).on('change', '#serviceDropdown', function () {
        updateSubmitButton();
    });

    // Submit form
    $('.submit-btn').on('click', function () {
        $(this).prop('disabled', true);
        $('#doctorForm').submit();
    });

    // Khởi tạo
    loadTimeSlots();
    updateSubmitButton();
});
