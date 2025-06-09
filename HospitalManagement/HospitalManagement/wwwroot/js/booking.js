//Để chọn được form: Bác sĩ hoặc Dịch vụ.
$('.toggle-btn').on('click', function () {
    $('.toggle-btn').removeClass('active');
    $(this).addClass('active');
    const type = $(this).data('type');
    $('#serviceForm').toggle(type === 'service');
    $('#doctorForm').toggle(type === 'doctor');
});

//Chọn ngày thì sẽ hiện ra bác sĩ rảnh ngày đó
$('#appointmentDate').on('change', function () {
    const selectedDate = $(this).val();
    if (!selectedDate) {
        $('#doctorList').empty(); // clear doctor cards
        $('#schedule').hide();
        $('.doctor-section').hide();
        return;
    }

    $.ajax({
        url: '/Appointment/GetDoctorsByDate',
        type: 'GET',
        data: { date: selectedDate },
        success: function (data) {
            $('#doctorList').empty(); // clear previous doctors
            if (data.length === 0) {
                $('#doctorList').append('<p class="text-muted text-center my-3">Không có bác sĩ trống trong ngày này</p>');
                $('.doctor-section').show();
                return;
            }

            data.forEach(function (doctor) {
                const card = `
							<div class="card text-center shadow-sm doctor-card" data-doctor-id="${doctor.doctorId}" style="min-width: 180px; cursor: pointer;">
								<div class="card-body">
									<img src="${doctor.profileImage ? 'data:image/png;base64,' + doctor.profileImage : '/img/logo.jpg'}"
										 class="img-fluid rounded-circle mb-2" style="width: 60px; height: 60px;" />
									<h6 class="card-title">${doctor.doctorName}</h6>
									<p class="text-muted small">${doctor.departmentName}</p>
								</div>
							</div>`;
                $('#doctorList').append(card);
            });
            $('.doctor-section').show();
        },
        error: function (xhr, status, error) {
            console.error("Error loading doctors: ", status, error);
        }
    });
});

//Set lại phần chọn bác sĩ sau mỗi lần render lại list bác sĩ 
$(document).on('click', '.doctor-card', function () {
    $('.doctor-card').removeClass('selected');
    $(this).addClass('selected');

    const doctorId = $(this).data('doctor-id');
    $('#SelectedDoctorId').val(doctorId);
    console.log("Doctor ID: " + doctorId);
    $('#chooseTimeTitle').show();
    const selectedDate = $('#appointmentDate').val();
    if (selectedDate) {
        loadTimeSlots(doctorId, selectedDate);
    }
});

function loadTimeSlots(doctorId, selectedDate) {
    if (!doctorId || !selectedDate) return;

    $('#time-slots-container').html('<div class="loading-slots"><div class="loading-spinner"></div>Đang tải...</div>');

    $.ajax({
        url: '/Appointment/GetSlotsByDoctorAndDate',
        method: 'GET',
        data: { doctorId, date: selectedDate },
        success: function (slots) {
            if (slots.length === 0) {
                $('#time-slots-container').html('<div class="no-slots"><i class="fas fa-calendar-times"></i><div>Không có khung giờ nào khả dụng</div></div>');
                return;
            }

            let html = '';
            slots.forEach(slot => {
                const slotClass = slot.isBooked ? 'time-slot booked' : 'time-slot available';
                const statusText = slot.isBooked ? 'Đã đặt' : 'Có thể đặt';
                const dotClass = slot.isBooked ? 'booked' : 'available';

                html += `
                    <div class="${slotClass}" data-slot-id="${slot.slotId}" data-time="${slot.slotTime}">
                        <div class="fw-semibold">${slot.slotTime}</div>
                        <div class="slot-status">
                            <span class="status-dot ${dotClass}"></span>
                            <span>${statusText}</span>
                        </div>
                    </div>
                `;
            });

            $('#time-slots-container').html(html);
        },
        error: function () {
            $('#time-slots-container').html('<div class="no-slots">Không thể tải dữ liệu khung giờ.</div>');
        }
    });
}

$(document).on('click', '.time-slot.available', function () {
    $('.time-slot.available').removeClass('selected');
    $(this).addClass('selected');
    const slotId = $(this).data('slot-id');
    $('#SelectedSlotId').val(slotId); // Hidden input dùng để submit form
    $('.submit-btn').prop('disabled', false);
    console.log("SlotID: " + slotId);
    $('.service-item').show();
});

$(document).on('change', 'select[name="SelectedServiceId"]', function () {
    const serviceId = $(this).val(); // lấy giá trị đã chọn
    console.log("Selected Service ID: " + serviceId);
    $('.note-area').show();
});
