/*
 * Additional function for chat.html
 *	Written by ThemePixels	
 *	http://themepixels.com/
 *
 *	Copyright (c) 2012 ThemePixels (http://themepixels.com)
 *	
 *	Built for Shamcey Premium Responsive Admin Template
 *  http://themeforest.net/category/site-templates/admin-templates
 */

jQuery(document).ready(function(){

	///// SEARCH USER FROM RIGHT SIDEBAR /////
	jQuery('.chatsearch input').bind('focusin focusout',function(e){
		if(e.type == 'focusin') {
			if(jQuery(this).val() == 'Search') jQuery(this).val('');	
		} else {
			if(jQuery(this).val() == '') jQuery(this).val('Search');	
		}
	});
	
	///// SUBMIT A MESSAGE USING ENTER KEY PRESS /////
	jQuery('.messagebox input').keypress(function(e){
		if(e.which == 13)
			enterMessage();
	});
	
	function enterMessage() {
		var msg = jQuery('.messagebox input').val(); //get the value of message box
		
		//display message from a message box
		if(msg != '') {
			jQuery('#chatmessageinner').append('<p><img src="images/chatthumb.png" alt="" />'
											   +'<span class="msgblock radius2"><strong>You</strong> <span class="time">- 10:14 am</span>'
											   +'<span class="msg">'+msg+'</span></span></p>');
			jQuery('.messagebox input').val('');
			jQuery('.messagebox input').focus();
			
			
			var he = jQuery('#chatmessage')[0].scrollHeight;
			jQuery('#chatmessage').scrollTop(he);
			
			//this will create a sample response display after submitting message
			window.setTimeout(  
				function() {  
					//this is just a sample reply when somebody send a message
					jQuery('#chatmessageinner').append('<p class="reply"><img src="images/chatthumb.png" alt="" />'
													   +'<span class="msgblock"><strong>She</strong> <span class="time">- 10:15 am</span>'
													   +'<span class="msg">This is an automated reply!!</span></span></p>');
					
					var he = jQuery('#chatmessage')[0].scrollHeight;
					jQuery('#chatmessage').scrollTop(he);
					
				}, 1000);			
		}	
	}
	
	
	// this will enable facebook like chat in every page
	
	if(jQuery.cookie('enable-chat'))
		jQuery('#enablechat').text('Disable');
	
		
	jQuery('#enablechat').click(function(){
		
		if(jQuery.cookie('enable-chat')) {
				
			jQuery.removeCookie('enable-chat', { path: '/' });
			jQuery(this).text('Enable');
				
		} else {
				
			jQuery.cookie('enable-chat', true, { path: '/' });
			jQuery(this).text('Disable');
			
		}
		
		location.reload();	
		return false;
	});
	
});
