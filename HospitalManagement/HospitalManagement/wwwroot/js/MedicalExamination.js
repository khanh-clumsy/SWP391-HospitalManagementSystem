﻿$(document).ready(function () {
    renderTrackingList();

    $('#testSelector').on('change', function () {
        let testId = $(this).val();
        console.log("Đã chọn TestID:", testId);
        if (testId === '') {
            $('#availableRoomListContainer').html('<select class="medical-form-select form-control flex-grow-1" id="roomSelector"><option value="">-- Vui lòng chọn loại xét nghiệm trước --</option></select>');
            return;
        }

        $.get('/Tracking/GetRoomsByTest', { testId }, function (rooms) {
            console.log(rooms);
            if (rooms && rooms.length > 0) {
                let roomOptions = '<select class="medical-form-select form-control flex-grow-1" id="roomSelector"><option value="">-- Vui lòng chọn phòng --</option>';
                rooms.forEach(function (room) {
                    roomOptions += `<option value="${room.roomId}" data-room-name="${room.roomName}" data-room-type="${room.roomType}">${room.roomName}</option>`;
                });
                roomOptions += '</select>';
                $('#availableRoomListContainer').html(roomOptions);
            } else {
                $('#availableRoomListContainer').html('<p class="text-danger">Không có phòng xét nghiệm nào phù hợp.</p>');
            }
        }).fail(function (xhr, status, error) {
            console.error("Error:", xhr.status, error);
            alert("Gọi API thất bại: " + xhr.status);
        });
    });

    $('#availableRoomListContainer').on('change', '#roomSelector', function () {
        let roomId = $(this).val();
        console.log("Đã chọn RoomID:", roomId);
    });

    $('form').on('submit', function (e) {
        if (trackings.length === 0) {
            e.preventDefault();
            alert('Vui lòng chỉ định ít nhất một xét nghiệm và phòng!');
        }
    });

    function renderTrackingList() {
        const $container = $('#assignedRoomList');

        if (trackings.length === 0) {
            $container.html(`
                <p class="text-muted text-center py-4">Chưa có phòng được chỉ định.</p>
            `);
            return;
        }

        let html = '<ul class="list-group">';
        trackings.forEach(tracking => {
            const testStatus = tracking.testStatus || 'Chưa rõ';
            if (tracking.roomType === 'Phòng khám') {
                html += `
                    <li class="list-group-item d-flex justify-content-between align-items-center">
                        <div>
                            <strong>${tracking.roomName} - ${tracking.roomType}</strong>
                        </div>
                    </li>
                `;
            } else {
                html += `
                    <li class="list-group-item d-flex justify-content-between align-items-center">
                        <div>
                            <strong>${tracking.roomName} - ${tracking.roomType} - ${tracking.testName || ''}</strong>
                            <br>
                            <span class="badge bg-secondary">Trạng thái: ${testStatus}</span>
                        </div>
                        <div class="d-flex align-items-center">
                            ${testStatus === 'Completed' ? `
                                <a href="/Tracking/TestDetail/${tracking.testListId}" class="btn btn-sm btn-outline-primary me-2">
                                    Xem kết quả
                                </a>
                            ` : ''}
                            
                        </div>
                    </li>
                `;
            }
        });
        html += '</ul>';
        $container.html(html);
        updateTestSelectOptions();
    }
    //<button type="button" class="btn btn-sm btn-outline-danger" onclick="removeTest(${tracking.testListId})">
    //    <i class="fas fa-times"></i>
    //</button>
    window.assignTest = function () {
        const $testSelect = $('#testSelector');
        const $roomSelect = $('#roomSelector');
        const testId = parseInt($testSelect.val());
        const roomId = $roomSelect.val();
        const appointmentId = $('input[name="AppointmentId"]').val();

        if (!testId || testId === '') {
            alert('Vui lòng chọn loại xét nghiệm!');
            return;
        }

        if (!roomId || roomId === '') {
            alert('Vui lòng chọn phòng xét nghiệm!');
            return;
        }

        if (trackings.find(t => t.testListId === parseInt(testId) && t.roomId === parseInt(roomId))) {
            alert('Xét nghiệm này với phòng đã chọn đã được thêm!');
            return;
        }

        console.log("Gửi dữ liệu tới server:");
        console.log("testId:", testId);
        console.log("roomId:", roomId);
        console.log("appointmentId:", appointmentId);

        $.ajax({
            url: '/Tracking/AssignRoom',
            type: 'POST',
            data: { roomId, appointmentId, testId },
            success: function (response) {
                console.log(response);
                const newTracking = {
                    testRecordId: response.testRecordId,
                    testId: testId, 
                    roomId: response.roomId,
                    roomName: response.roomName,
                    roomType: response.roomType,
                    testName: response.testName,
                    testStatus: response.testStatus
                };
                trackings.push(newTracking);
                renderTrackingList();
                $testSelect.prop('selectedIndex', 0).trigger('change');
                alert("Thêm phòng thành công ✅");
            },
            error: function (xhr) {
                console.error("Error:", xhr.status, xhr.responseText);
                const errMsg = xhr.responseJSON?.message || "Đã xảy ra lỗi.";
                alert("⚠️ " + errMsg);
            }
        });
    };

    function updateTestSelectOptions() {
        const $select = $('#testSelector');
        const selectedTestIds = trackings.map(t => parseInt(t.testId));

        $select.find('option').each(function () {
            const option = $(this);
            const optionVal = parseInt(option.val());

            if (isNaN(optionVal)) return; // Bỏ qua option "-- Chọn loại xét nghiệm --"

            if (selectedTestIds.includes(optionVal)) {
                option.hide();
            } else {
                option.show();
            }
        });
        $select.prop('selectedIndex', 0);
    }

});
