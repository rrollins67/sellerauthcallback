SP API Authorization Flow

Click 'Authorize Shipica to Communicate with Amazon' button in oxWebApp

1. upsert into sp_api_credentials table for customer_id (selling_partner_id?, auth_requested_date) 
 do we have access to selling_partner_id at this point? thats what the amazon callback gives me, and i need something to identify which customer the callback is for
 so i can update the table. i can either write the selling_partner_id here, or make a call to map it to the customer_id in the callback. i am assuming customer_id maps 1:1
 with selling_partner_id) 
2. redirect customer to https://sellercentral.amazon.com/apps/authorize/consent?application_id=amzn1.sp.solution.2275f979-dd40-401d-bd7c-10a01598f375
3. customer will login and authorize the Shipica application, which will trigger a callback.

Azure Function SellerAuthCallback.LWACallback receives callback from amazon (i am doing this in a Function so that i dont have to map it back to oxWebApp. that requires javascript and 
 an unfamiliar process. i know how to do an api in a Function)
1. update sp_api_credentials for most recent selling_partner_id (selling_partner_id, mws_auth_token, spapi_oauth_code)
 optional. this will let us know that we got the callback . mws_auth_token is the old token you are using now. maybe we can use it to map if selling_partner_id does not work
2. call LWA authorization server (https://api.amazon.com/auth/o2/token) to get a refresh token
 i need the client_id and client_secret here. they are part of your LWA creds. do you have them?
3. update sp_api_credentials for selling_partner_id (lwa_refresh_token, auth_updated_date)
 now we have refresh_token. i think thats all we need for this flow
 
refresh_token (LWA Refresh Token) is long lived. cant find an explicit reference, but i think it lasts forever. every time you want to call the SP API,
 you will need to use that Refresh token to get a fresh Access Token, which is valid for 1 hour. so you will need access to the Refresh token for many of your processes i believe. 
 

CREATE TABLE sp_api_credentials (
  customer_id,
  selling_partner_id,
  mws_auth_token,
  spapi_oauth_code,
  lwa_refresh_token,
  auth_requested_date,
  auth_granted_date,
  lwa_refresh_token_date
);