$(document).ready(function () {


    $.urlParam = function (name) {
        var results = new RegExp('[\?&]' + name + '=([^&#]*)')
                          .exec(window.location.search);

        return results[1] || 0;
    }

    checkStartFromParameter();


    var partialView_ServerConnection =
        '<div class="serverLink" id="server_{0}" style="width:80px;height:100px; text-align:center" data-serverid="{1}">' +
            '<button class="rdpButton" id="connect" value=" " title="open session" />' +
            '<label id="serverName" class="{2}" title="{3}">{4}</label>{5}' +
        '</div>';

    // LOGIN
    $('.enterpriseLogin').click(function () {
        var button = $(this);
        var err = $('#loginError');
        err.text('');

        $(button).prop('disabled', true);
        $('#serverListDiv').empty();
        // call the logini request passing in authentication details
        var credentials = {
            loginRequest: {
                Server: $('#server').val(),
                Domain: $('#domain').val(),
                Username: $('#username').val(),
                Password: $('#password').val(),
                MFAPassword: $('#mfaPassword').val(),
                Program: $('#program').val()
            }
        }

        $.ajax({
            url: "default.aspx/ConnectLogin",
            type: "POST",
            data: JSON.stringify(credentials),
            dataType: "json",
            contentType: "application/json; charset=utf-8"
        })
        .done(function (loginRequest) {
            if (loginRequest.d.Success) {
                $('#server').val('');
                $('#domain').val('');
                $('#program').val('');
                $('#username').val('');
                $('#password').val('');
                $('#mfaPassword').val('');
                // display requried sections based on api return values
                $('#enterpriseSessionID').val(loginRequest.d.EnterpriseSessionID);
                $('#enterpriseSessionKey').val(loginRequest.d.EnterpriseSessionKey);
                
                // add servers to display to users
                $.each(loginRequest.d.ServerList, function (index, serverConfiguration) {
                    addHost(serverConfiguration.ServerId,
                        serverConfiguration.ServerName,
                        loginRequest.d.IsAdmin);
                });

                // display servers section                
                $('#loginScreen').animate({ left: "-150%" }, 500, function () {
                    $('#newHost').toggle(loginRequest.d.IsAdmin);
                    $('#loginScreen').toggle(false);
                    $('#enterpriseDiv').toggle(true);
                    $('#newHost').toggle(loginRequest.d.IsAdmin);
                    $('#serverContainer').animate({ left: "50%" }, 800);
                });

            } else {
                $('#password').val('');
                $('#mfaPassword').val('');
                err.text(loginRequest.d.Message);
            }
            $(button).prop('disabled', false);
        })
        .fail(function (error) {
            $('#password').val('');
            $('#mfaPassword').val('');
            err.text(error);
            $(button).prop('disabled', false);
        });
    });

    $('.standardLogin').click(function () {
        var btn = $(this);
        var err = $('#loginError');
        err.text('');

        $(btn).prop('disabled', true);

        $('#toolbar').show();

        // grab screen sizes
        var display = new Display();

        // call the logini request passing in authentication details
        var startSessionRequest = {
            startSessionRequest: {
                Server: $('#server').val(),
                Domain: $('#domain').val(),
                Username: $('#username').val(),
                Password: $('#password').val(),
                MFAPassword: $('#mfaPassword').val(),
                Width: display.getBrowserWidth() - display.getHorizontalOffset(),
                Height: display.getBrowserHeight() - display.getVerticalOffset() - 16,
                ProgramValue: ''
            }
        }

        $.ajax({
            url: "default.aspx/StartServerSession",
            type: "POST",
            data: JSON.stringify(startSessionRequest),
            dataType: "json",
            contentType: "application/json; charset=utf-8"
        })
        .done(function (sessionStartResponse) {

            if (sessionStartResponse.d.Success) {
                
                $('#enterpriseSessionID').val('');
                $('#enterpriseSessionKey').val('');
                $('#serverInfo').val(sessionStartResponse.d.RemoteSessionDetails.ServerAddress);

                startMyrtille(true,
                            false,
                            false,
                            false,
                            false,
                            sessionStartResponse.d.RemoteSessionDetails.ClientWidth,
                            sessionStartResponse.d.RemoteSessionDetails.ClientHeight);
            } else {
                err.text(sessionStartResponse.d.Message);
                $('#toolbar').hide();
            }

            $(btn).prop('disabled', false);
        })
        .fail(function (error) {
            err.text(error);
            $(btn).prop('disabled', false);
            $('#toolbar').hide();
        });
        return false;
    })

    //Save Host
    $('#saveHost').click(function () {
        var btn = $(this);
        btn.prop('disabled', true);
        var hostID = $('#editHostID').val();

        if (hostID == '') hostID = null;

        var hostDetails = {
            addHostRequest: {
                SessionID: $('#enterpriseSessionID').val(),
                HostID: hostID,
                HostName: $('#editHostname').val(),
                HostAddress: $('#editHostaddress').val(),
                DirectoryGroups: $('#editgroupaccess').val()
            }
        };

        $.ajax({
            url: "default.aspx/SaveHost",
            type: "POST",
            data: JSON.stringify(hostDetails),
            dataType: "json",
            contentType: "application/json; charset=utf-8"
        })
        .done(function (addHostResponse) {
            if (addHostResponse.d.Success) {
                if (hostID == null) {
                    addHost(addHostResponse.d.ServerId,
                            addHostResponse.d.ServerName,
                            true);
                }
                closeModal('popupHost');
            } else {
                alert(addHostResponse.d.Message);
            }
            btn.prop('disabled', false);
        })
        .fail(function (error) {
            alert(error);
            btn.prop('disabled', false);
        });
        
    });

    $('#deleteHost').click(function () {
        var btn = $(this);
        btn.prop('disabled', true);
        var deleteReq = {
            deleteHostRequest: {
                HostID: $('#editHostID').val(),
                SessionID: $('#enterpriseSessionID').val()
            }
        }

        $.ajax({
            url: "default.aspx/DeleteHost",
            type: "POST",
            data: JSON.stringify(deleteReq),
            dataType: "json",
            contentType: "application/json; charset=utf-8"
        })
       .done(function (delHostResponse) {
           if (delHostResponse.d.Success) {
               
               $('#server_' + delHostResponse.d.HostID).remove();

               closeModal('popupHost');
           } else {
               alert(delHostResponse.d.Message);
           }
           btn.prop('disabled', false);
       })
       .fail(function (error) {
           alert(error);
           btn.prop('disabled', false);
       });
    });

    $('.logout').click(function(){
        var btn = $(this);
        btn.prop('disabled',true);
        logout(btn);
    });

    $('#createUserSession').click(function () {
        if ($('#sessionURL').val() != '') return;

        var hostID = $('#editHostID').val();
        var btn = $(this);
        btn.prop('disabled', true);
 
        var sessionDetails = {
            createSessionRequest: {
                SessionID: $('#enterpriseSessionID').val(),
                HostID: hostID,
                Username: $('#sessionUsername').val(),
                Password: $('#sessionPassword').val(),
            }
        };

        $.ajax({
            url: "default.aspx/CreateSession",
            type: "POST",
            data: JSON.stringify(sessionDetails),
            dataType: "json",
            contentType: "application/json; charset=utf-8"
        })
        .done(function (createUserSessionHttpResponse) {
            if (createUserSessionHttpResponse.d.Success) {
                $('#sessionURL').val(createUserSessionHttpResponse.d.SessionURL);
            } else {
                alert(createUserSessionHttpResponse.d.Message);
            }
            btn.prop('disabled', false);
        })
        .fail(function (error) {
            alert(error);
            btn.prop('disabled', false);
        });
    });

    window.addEventListener("beforeunload", function (e) {
        logout();
    });

    function addHost(serverId, serverName, isAdmin) {

        var serverHtml = format(partialView_ServerConnection,
                        serverId,
                        serverId,
                        (isAdmin ? "editServer" : ""),
                        (isAdmin ? "Edit Server" : ""),
                        serverName,
                        ""
                        );

        $('#serverListDiv').append(serverHtml);
    }

    function logout(btn)
    {
        // call the logini request passing in authentication details
        var logoutDetails = {
            logoutRequest: {
                EnterpriseSessionId: $('#enterpriseSessionID').val()
            }
        }
        $.ajax({
            url: "default.aspx/ConnectLogout",
            type: "POST",
            data: JSON.stringify(logoutDetails),
            dataType: "json",
            contentType: "application/json; charset=utf-8"
        })
        .done(function (logoutResponse) {
            $('#enterpriseSessionID').val('');
            $('#enterpriseSessionKey').val('');
            $('#newHost').hide();
            $('#toolbar').hide();
            if (logoutResponse.d.Success) {
                // display requried sections based on api return values
                $('#enterpriseUser').val('');
                $('#enterpriseSessionID').val('');
                $('#enterpriseSessionKey').val('');

                // add servers to display to users                
                $('#loginScreen').toggle(true);
                $('#serverContainer').animate({ left: "150%" }, 800, function () {
                    $('#loginScreen').animate({ left: "50%" }, 800, function () {
                        $('#newHost').toggle(false);
                        $('#enterpriseDiv').toggle(false);
                        $('#newHost').toggle(false);
                        $('#displayDiv').delay(1000).empty();
                    });
                });
            } else {
                alert(logoutResponse.d.Message);
            }
            $(btn).prop('disabled', false);
        })
        .fail(function (error) {
            $(btn).prop('disabled', false);
        });
    }

    function checkStartFromParameter()
    {
        try
        {
            var si = $.urlParam('SI');
            var sk = $.urlParam('SK');
            var sd = $.urlParam('SD');
            var err = $('#loginError');
            err.text('');

            $('#toolbar').show();

            // grab screen sizes
            var display = new Display();

            // call the logini request passing in authentication details
            var startSessionRequest = {
                startSessionRequest: {
                    SessionID: si,
                    SessionKey: sk,
                    ServerID: sd,
                    Width: display.getBrowserWidth() - display.getHorizontalOffset(),
                    Height: display.getBrowserHeight() - display.getVerticalOffset(),
                    ProgramValue: ''
                }
            }

            $.ajax({
                url: "default.aspx/StartServerSession",
                type: "POST",
                data: JSON.stringify(startSessionRequest),
                dataType: "json",
                contentType: "application/json; charset=utf-8"
            })
            .done(function (sessionStartResponse) {

                if (sessionStartResponse.d.Success) {

                    $('#enterpriseSessionID').val('');
                    $('#enterpriseSessionKey').val('');
                    $('#serverInfo').val(sessionStartResponse.d.RemoteSessionDetails.ServerAddress);

                    startMyrtille(true,
                                false,
                                false,
                                false,
                                false,
                                sessionStartResponse.d.RemoteSessionDetails.ClientWidth,
                                sessionStartResponse.d.RemoteSessionDetails.ClientHeight);
                } else {
                    err.text(sessionStartResponse.d.Message);
                    $('#toolbar').hide();
                }

            })
            .fail(function (error) {
                err.text(error);
                $('#toolbar').hide();
            });
        }catch(e){

        }
    }

    function getWebMethodBase(methodName)
    {
        var pathname = '';
        var parts = new Array();
        parts = window.location.pathname.split('/');
        for (var i = 0; i < parts.length - 1; i++) {
            if (parts[i] != '') {
                if (pathname == '') {
                    pathname = parts[i];
                }
                else {
                    pathname += '/' + parts[i];
                }
            }
        }

        return "/" + pathname + "/Default.aspx/" + methodName;
    }
});

$(document).on('click', '.editServer', function () {
    var ctl = $(this);
    $(ctl).prop('disabled', true);
    var hostID = ctl.parent().data('serverid');

    var editReq = {
        editHostRequest: {
            HostID: hostID,
            SessionID: $('#enterpriseSessionID').val()
        }
    }

    $.ajax({
        url: "default.aspx/GetHost",
        type: "POST",
        data: JSON.stringify(editReq),
        dataType: "json",
        contentType: "application/json; charset=utf-8"
    })
   .done(function (editHostResponse) {
       if (editHostResponse.d.Success) {
           showHostModal(true);
           $('#editHostID').val(editHostResponse.d.HostID);
           $('#editHostname').val(editHostResponse.d.HostName);
           $('#editHostaddress').val(editHostResponse.d.HostAddress);
           $('#editgroupaccess').val(editHostResponse.d.DirectoryGroups);
           
       } else {
           alert(editHostResponse.d.Message);
       }
       ctl.prop('disabled', false);
   })
   .fail(function (error) {
       alert(error);
       ctl.prop('disabled', false);
   });
});

$(document).on('click', '.rdpButton', function () {
    var btn = $(this);

    $(btn).prop('disabled', true);
    $('#newHost').hide();
    
    $('#toolbar').show();

    // grab screen sizes
    var display = new Display();

    // call the logini request passing in authentication details
    var startSessionRequest = {
        startSessionRequest: {
            SessionID: $('#enterpriseSessionID').val(),
            SessionKey:  $('#enterpriseSessionKey').val(),
            ServerID: $(this).parent().data('serverid'),
            Width: display.getBrowserWidth() - display.getHorizontalOffset(),
            Height: display.getBrowserHeight() - display.getVerticalOffset(),
            ProgramValue: ''
        }
    }

    $.ajax({
        url: "default.aspx/StartServerSession",
        type: "POST",
        data: JSON.stringify(startSessionRequest),
        dataType: "json",
        contentType: "application/json; charset=utf-8"
    })
    .done(function (sessionStartResponse) {
        //TODO: Not sure this is the correct thing to do PJO
        //$('#serverContainer').hide();

        if (sessionStartResponse.d.Success) {

            $('#enterpriseSessionID').val('');
            $('#enterpriseSessionKey').val('');
            $('#serverInfo').val(sessionStartResponse.d.RemoteSessionDetails.ServerAddress);

            startMyrtille(true,
                        false,
                        false,
                        false,
                        false,
                        sessionStartResponse.d.RemoteSessionDetails.ClientWidth,
                        sessionStartResponse.d.RemoteSessionDetails.ClientHeight);
        } else {
            alert(sessionStartResponse.d.Message);
            $('.logout').click();
        }

        $(btn).prop('disabled', false);
    })
    .fail(function (error) {
        alert(error);
        $(btn).prop('disabled', false);
    });
    return false;
});

// provide c# .net string.format type formatting
var format = function (str, col) {
    col = typeof col === 'object' ? col : Array.prototype.slice.call(arguments, 1);

    return str.replace(/\{\{|\}\}|\{(\w+)\}/g, function (m, n) {
        if (m == "{{") { return "{"; }
        if (m == "}}") { return "}"; }
        return col[n];
    });
};

function disableControl(controlId) {
    var control = document.getElementById(controlId);
    if (control != null) {
        control.disabled = true;
    }
}

function disableToolbar() {
    disableControl('stat');
    disableControl('debug');
    disableControl('browser');
    disableControl('scale');
    disableControl('keyboard');
    disableControl('clipboard');
    disableControl('files');
    disableControl('cad');
    disableControl('logout');
}

function showHostModal(isEdit) {
    clearHostForm();
    if (isEdit) {
        $('#createSession').show();
        $('#deleteHost').show();
    }
    $('#popupHost').fadeIn(150);
}

function closeModal(modalID) {
    $('#' + modalID).fadeOut(150);
    switch(modalID)
    {
        case "popupSession": clearSessionForm(); break;
        case "popupHost": clearHostForm(); break;
    }
    
}

function clearHostForm() {
    $('#editHostID').val('');
    $('#editHostname').val('');
    $('#editHostaddress').val('');
    $('#editgroupaccess').val('');
    $('#createSession').hide();
    $('#deleteHost').hide();
}

function showCreateSessionModal()
{
    $('#sessionHostnameLabel').text($('#editHostname').val());
    $('#sessionUsername').val('');
    $('#sessionPassword').val('');
    $('#sessionURL').val('');
    $('#popupSession').fadeIn(150);
}

function clearSessionForm()
{
    $('#sessionHostnameLabel').text('');
    $('#sessionUsername').val('');
    $('#sessionPassword').val('');
    $('#sessionURL').val('');
}